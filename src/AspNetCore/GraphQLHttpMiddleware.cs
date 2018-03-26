using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpMiddleware<TSchema> where TSchema : ISchema
    {
        private const string JsonContentType = "application/json";
        private const string GraphQLContentType = "application/graphql";

        private readonly RequestDelegate _next;
        private readonly GraphQLHttpOptions _options;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;
        private readonly TSchema _schema;

        public GraphQLHttpMiddleware(
            RequestDelegate next,
            IOptions<GraphQLHttpOptions> options,
            IDocumentExecuter executer,
            IDocumentWriter writer,
            TSchema schema)
        {
            _next = next;
            _options = options.Value;
            _executer = executer;
            _writer = writer;
            _schema = schema;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!IsGraphQlRequest(context))
            {
                await _next(context);
                return;
            }

            await ExecuteAsync(context, _schema);
        }

        private bool IsGraphQlRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(_options.Path);
        }

        private async Task ExecuteAsync(HttpContext context, ISchema schema)
        {
            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            var request = new GraphQLQuery();
            if (HttpMethods.IsGet(context.Request.Method))
            {
                var qs = context.Request.Query;
                request.Query = qs.TryGetValue(GraphQLQuery.QueryKey, out StringValues queryValues) ? queryValues[0] : null;
                request.Variables = qs.TryGetValue(GraphQLQuery.VariablesKey, out StringValues variablesValues) ? JObject.Parse(variablesValues[0]) : null;
                request.OperationName = qs.TryGetValue(GraphQLQuery.OperationNameKey, out StringValues operationNameValues) ? operationNameValues[0] : null;
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                switch (context.Request.ContentType)
                {
                    case JsonContentType:
                        request = Deserialize<GraphQLQuery>(context.Request.Body);
                        break;
                    case GraphQLContentType:
                        request.Query = await ReadAsStringAsync(context.Request.Body);
                        break;
                }                
            }
            
            object userContext = null;
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
            if (userContextBuilder != null)
            {
                userContext = await userContextBuilder.BuildUserContext(context);
            }
            else
            {
                userContext = _options.BuildUserContext?.Invoke(context);
            }
            
            var result = await _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = request.Query;
                _.OperationName = request.OperationName;
                _.Inputs = request.Variables.ToInputs();
                _.UserContext = userContext;
                _.ExposeExceptions = _options.ExposeExceptions;
                _.ValidationRules = _options.ValidationRules.Concat(DocumentValidator.CoreRules()).ToList();
            });

            await WriteResponseAsync(context, result);
        }

        private async Task WriteResponseAsync(HttpContext context, ExecutionResult result)
        {
            var json = _writer.Write(result);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;

            await context.Response.WriteAsync(json);
        }

        private static T Deserialize<T>(Stream s)
        {
            using (var reader = new StreamReader(s))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return new JsonSerializer().Deserialize<T>(jsonReader);
            }
        }

        private static async Task<string> ReadAsStringAsync(Stream s)
        {
            using (var reader = new StreamReader(s))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}

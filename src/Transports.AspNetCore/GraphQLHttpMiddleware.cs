using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Core;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpMiddleware<TSchema> : IMiddleware
        where TSchema : ISchema
    {
        private const string JsonContentType = "application/json";
        private const string GraphQLContentType = "application/graphql";

        private readonly IGraphQLExecuter<TSchema> _executer;
        private readonly IDocumentWriter _writer;

        public GraphQLHttpMiddleware(
            IGraphQLExecuter<TSchema> executer,
            IDocumentWriter writer)
        {
            _executer = executer;
            _writer = writer;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await next(context);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            var httpRequest = context.Request;
            var gqlRequest = new GraphQLRequest();

            if (HttpMethods.IsGet(httpRequest.Method) || (HttpMethods.IsPost(httpRequest.Method) && httpRequest.Query.ContainsKey(GraphQLRequest.QueryKey)))
            {
                ExtractGraphQLRequestFromQueryString(httpRequest.Query, gqlRequest);
            }
            else if (HttpMethods.IsPost(httpRequest.Method))
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out MediaTypeHeaderValue mediaTypeHeader))
                {
                    await WriteResponseAsync(context, HttpStatusCode.BadRequest, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.");
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case JsonContentType:
                        gqlRequest = Deserialize<GraphQLRequest>(httpRequest.Body);
                        break;
                    case GraphQLContentType:
                        gqlRequest.Query = await ReadAsStringAsync(httpRequest.Body);
                        break;
                    default:
                        await WriteResponseAsync(context, HttpStatusCode.BadRequest, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{JsonContentType}' or '{GraphQLContentType}'. See: http://graphql.org/learn/serving-over-http/.");
                        return;
                }
            }

            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
            object userContext = await userContextBuilder?.BuildUserContext(context);

            var result = await _executer.ExecuteAsync(
                gqlRequest.OperationName,
                gqlRequest.Query,
                gqlRequest.GetInputs(),
                userContext);

            await WriteResponseAsync(context, result);
        }

        private async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string errorMessage)
        {
            var result = new ExecutionResult()
            {
                Errors = new ExecutionErrors()
            };
            result.Errors.Add(new ExecutionError(errorMessage));

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

        private static void ExtractGraphQLRequestFromQueryString(IQueryCollection qs, GraphQLRequest gqlRequest)
        {
            gqlRequest.Query = qs.TryGetValue(GraphQLRequest.QueryKey, out StringValues queryValues) ? queryValues[0] : null;
            gqlRequest.Variables = qs.TryGetValue(GraphQLRequest.VariablesKey, out StringValues variablesValues) ? JObject.Parse(variablesValues[0]) : null;
            gqlRequest.OperationName = qs.TryGetValue(GraphQLRequest.OperationNameKey, out StringValues operationNameValues) ? operationNameValues[0] : null;
        }
    }
}

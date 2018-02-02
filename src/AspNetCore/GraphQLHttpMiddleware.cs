using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpMiddleware<TSchema> where TSchema : ISchema
    {
        private readonly RequestDelegate _next;
        private readonly GraphQLHttpOptions _options;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;
        private readonly TSchema _schema;
        private readonly IEnumerable<IDocumentExecutionListener> _documentListners;

        public GraphQLHttpMiddleware(
            RequestDelegate next,
            IOptions<GraphQLHttpOptions> options,
            IDocumentExecuter executer,
            IDocumentWriter writer,
            TSchema schema,
            IEnumerable<IDocumentExecutionListener> documentListners)
        {
            _next = next;
            _options = options.Value;
            _executer = executer;
            _writer = writer;
            _schema = schema;
            _documentListners = documentListners;
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
            var request = Deserialize<GraphQLQuery>(context.Request.Body);

            var result = await _executer.ExecuteAsync(_ =>
            {
                _.Schema = schema;
                _.Query = request.Query;
                _.OperationName = request.OperationName;
                _.Inputs = request.Variables.ToInputs();
                _.UserContext = _options.BuildUserContext?.Invoke(context);
                _.ExposeExceptions = _options.ExposeExceptions;
                _.ValidationRules = _options.ValidationRules.Concat(DocumentValidator.CoreRules()).ToList();
                _documentListners
                    .ToList()
                    .ForEach(_.Listeners.Add);
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
                var ser = new JsonSerializer();
                return ser.Deserialize<T>(jsonReader);
            }
        }
    }
}

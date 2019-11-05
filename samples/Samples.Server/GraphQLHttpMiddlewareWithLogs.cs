using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.Samples.Server
{
    // Example of a custom GraphQL Middleware that sends execution result to Microsoft.Extensions.Logging API
    public class GraphQLHttpMiddlewareWithLogs<TSchema> : GraphQLHttpMiddleware<TSchema>
        where TSchema : ISchema
    {
        private readonly ILogger _logger;

        public GraphQLHttpMiddlewareWithLogs(ILogger<GraphQLHttpMiddleware<TSchema>> logger, RequestDelegate next, PathString path, Action<JsonSerializerSettings> configure)
            : base(next, path, configure)
        {
            _logger = logger;
        }

        protected override Task RequestExecutedAsync(GraphQLRequest request, int indexInBatch, ExecutionResult result)
        {
            if (result.Errors != null)
            {
                if (indexInBatch >= 0)
                    _logger.LogError("GraphQL execution error(s) in batch [{Index}]: {Errors}", indexInBatch, result.Errors);
                else
                    _logger.LogError("GraphQL execution error(s): {Errors}", result.Errors);
            }

            return base.RequestExecutedAsync(request, indexInBatch, result);
        }

        protected override CancellationToken GetCancellationToken(HttpContext context)
        {
            // custom CancellationToken example 
            var cts = CancellationTokenSource.CreateLinkedTokenSource(base.GetCancellationToken(context), new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            return cts.Token;
        }
    }
}

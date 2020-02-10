using GraphQL.Instrumentation;
using GraphQL.Server.Common;
using GraphQL.Server.Internal;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpMiddleware<TSchema>
        where TSchema : ISchema
    {
        private const string JsonContentType = "application/json";
        private const string GraphQLContentType = "application/graphql";
        private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";

        private readonly RequestDelegate _next;
        private readonly PathString _path;
        private readonly IGraphQLRequestDeserializer _deserializer;

        public GraphQLHttpMiddleware(RequestDelegate next, PathString path, IGraphQLRequestDeserializer deserializer)
        {
            _next = next;
            _path = path;
            _deserializer = deserializer;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest || !context.Request.Path.StartsWithSegments(_path))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            var httpRequest = context.Request;
            GraphQLRequest gqlRequest = null;
            GraphQLRequest[] gqlBatchRequest = null;

            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();
            var cancellationToken = GetCancellationToken(context);

            if (HttpMethods.IsGet(httpRequest.Method) ||
                (HttpMethods.IsPost(httpRequest.Method) && httpRequest.Query.ContainsKey(GraphQLRequest.QueryKey)))
            {
                gqlRequest = _deserializer.DeserializeFromQueryString(httpRequest.Query);
            }
            else if (HttpMethods.IsPost(httpRequest.Method))
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
                {
                    await WriteBadRequestResponseAsync(context, writer, cancellationToken, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.").ConfigureAwait(false);
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case JsonContentType:
                        var deserializationResult = await _deserializer.DeserializeFromJsonBodyAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                        if (!deserializationResult.IsSuccessful)
                        {
                            await WriteBadRequestResponseAsync(context, writer, cancellationToken, "Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query.").ConfigureAwait(false);
                            return;
                        }
                        gqlRequest = deserializationResult.Single;
                        gqlBatchRequest = deserializationResult.Batch;
                        break;

                    case GraphQLContentType:
                        gqlRequest.Query = await ReadAsStringAsync(httpRequest.Body).ConfigureAwait(false);
                        break;

                    case FormUrlEncodedContentType:
                        var formCollection = await httpRequest.ReadFormAsync().ConfigureAwait(false);
                        gqlRequest = _deserializer.DeserializeFromFormBody(formCollection);
                        break;

                    default:
                        await WriteBadRequestResponseAsync(context, writer, cancellationToken, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{JsonContentType}', '{GraphQLContentType}', or '{FormUrlEncodedContentType}'. See: http://graphql.org/learn/serving-over-http/.").ConfigureAwait(false);
                        return;
                }
            }

            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
            IDictionary<string, object> userContext = userContextBuilder == null
                ? new Dictionary<string, object>() // in order to allow resolvers to exchange their state through this object
                : await userContextBuilder.BuildUserContext(context).ConfigureAwait(false);

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();

            // normal execution with single graphql request
            if (gqlBatchRequest == null)
            {
                var stopwatch = ValueStopwatch.StartNew();
                var result = await ExecuteRequestAsync(gqlRequest, userContext, executer, cancellationToken);

                await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequest, result, stopwatch.Elapsed));

                await WriteResponseAsync(context, writer, cancellationToken, result).ConfigureAwait(false);
            }
            // execute multiple graphql requests in one batch
            else
            {
                var executionResults = new ExecutionResult[gqlBatchRequest.Length];
                for (int i = 0; i < gqlBatchRequest.Length; ++i)
                {
                    var request = gqlBatchRequest[i];

                    var stopwatch = ValueStopwatch.StartNew();
                    var result = await ExecuteRequestAsync(request, userContext, executer, cancellationToken);

                    await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequest, result, stopwatch.Elapsed, i));

                    executionResults[i] = result;
                }

                await WriteResponseAsync(context, writer, cancellationToken, executionResults).ConfigureAwait(false);
            }
        }

        private static async Task<ExecutionResult> ExecuteRequestAsync(GraphQLRequest gqlRequest, IDictionary<string, object> userContext, IGraphQLExecuter<TSchema> executer, CancellationToken token)
            => await executer.ExecuteAsync(
                gqlRequest.OperationName,
                gqlRequest.Query,
                gqlRequest.GetInputs(),
                userContext,
                token).ConfigureAwait(false);

        protected virtual CancellationToken GetCancellationToken(HttpContext context) => context.RequestAborted;

        protected virtual Task RequestExecutedAsync(in GraphQLRequestExecutionResult requestExecutionResult)
        {
            // nothing to do in this middleware
            return Task.CompletedTask;
        }

        private Task WriteBadRequestResponseAsync(HttpContext context, IDocumentWriter writer, CancellationToken cancellationToken, string errorMessage)
        {
            var result = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError(errorMessage)
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 400; // Bad Request

            return writer.WriteAsync(context.Response.Body, result, cancellationToken);
        }

        private Task WriteResponseAsync<TResult>(HttpContext context, IDocumentWriter writer, CancellationToken cancellationToken, TResult result)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200; // OK

            return writer.WriteAsync(context.Response.Body, result, cancellationToken);
        }

        private static async Task<string> ReadAsStringAsync(Stream s)
        {
            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            return await new StreamReader(s).ReadToEndAsync().ConfigureAwait(false);
        }
    }
}

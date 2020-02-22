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
        private const string DOCS_URL = "See: http://graphql.org/learn/serving-over-http/.";

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
                await _next(context);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            // Inspiration: https://github.com/graphql/express-graphql/blob/master/src/index.js
            var httpRequest = context.Request;
            var httpResponse = context.Response;

            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();
            var cancellationToken = GetCancellationToken(context);

            // GraphQL HTTP only supports GET and POST methods
            bool isGet = HttpMethods.IsGet(httpRequest.Method);
            bool isPost = HttpMethods.IsPost(httpRequest.Method);
            if (!isGet && !isPost)
            {
                httpResponse.Headers["Allow"] = "GET, POST";
                await WriteErrorResponseAsync(httpResponse, writer, cancellationToken,
                    $"Invalid HTTP method. Only GET and POST are supported. {DOCS_URL}",
                    httpStatusCode: 405 // Method Not Allowed
                );
                return;
            }

            // Parse POST body
            GraphQLRequest bodyGQLRequest = null;
            GraphQLRequest[] bodyGQLBatchRequest = null;
            if (isPost)
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
                {
                    await WriteErrorResponseAsync(httpResponse, writer, cancellationToken, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.");
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case MediaType.JSON:
                        var deserializationResult = await _deserializer.DeserializeFromJsonBodyAsync(httpRequest, cancellationToken);
                        if (!deserializationResult.IsSuccessful)
                        {
                            await WriteErrorResponseAsync(httpResponse, writer, cancellationToken, "Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query.");
                            return;
                        }
                        bodyGQLRequest = deserializationResult.Single;
                        bodyGQLBatchRequest = deserializationResult.Batch;
                        break;

                    case MediaType.GRAPH_QL:
                        bodyGQLRequest = await DeserializeFromGraphBodyAsync(httpRequest.Body);
                        break;

                    case MediaType.FORM:
                        var formCollection = await httpRequest.ReadFormAsync();
                        bodyGQLRequest = DeserializeFromFormBody(formCollection);
                        break;

                    default:
                        await WriteErrorResponseAsync(httpResponse, writer, cancellationToken, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{MediaType.JSON}', '{MediaType.GRAPH_QL}' or '{MediaType.FORM}'. {DOCS_URL}");
                        return;
                }
            }

            // If we don't have a batch request, parse the URL too to determine the actual request to run
            // Querystring params take priority
            GraphQLRequest gqlRequest = null;
            if (bodyGQLBatchRequest == null)
            {
                // Parse URL
                var urlGQLRequest = DeserializeFromQueryString(httpRequest.Query);

                gqlRequest = new GraphQLRequest
                {
                    Query = urlGQLRequest.Query ?? bodyGQLRequest?.Query,
                    Inputs = urlGQLRequest.Inputs ?? bodyGQLRequest?.Inputs,
                    OperationName = urlGQLRequest.OperationName ?? bodyGQLRequest?.OperationName
                };
            }

            // Prepare context and execute
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
            var userContext = userContextBuilder == null
                ? new Dictionary<string, object>() // in order to allow resolvers to exchange their state through this object
                : await userContextBuilder.BuildUserContext(context);

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();

            // Normal execution with single graphql request
            if (bodyGQLBatchRequest == null)
            {
                var stopwatch = ValueStopwatch.StartNew();
                var result = await ExecuteRequestAsync(gqlRequest, userContext, executer, cancellationToken);

                await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequest, result, stopwatch.Elapsed));

                await WriteResponseAsync(httpResponse, writer, cancellationToken, result);
            }
            // Execute multiple graphql requests in one batch
            else
            {
                var executionResults = new ExecutionResult[bodyGQLBatchRequest.Length];
                for (int i = 0; i < bodyGQLBatchRequest.Length; ++i)
                {
                    var gqlRequestInBatch = bodyGQLBatchRequest[i];

                    var stopwatch = ValueStopwatch.StartNew();
                    var result = await ExecuteRequestAsync(gqlRequestInBatch, userContext, executer, cancellationToken);

                    await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequestInBatch, result, stopwatch.Elapsed, i));

                    executionResults[i] = result;
                }

                await WriteResponseAsync(httpResponse, writer, cancellationToken, executionResults);
            }
        }

        private static Task<ExecutionResult> ExecuteRequestAsync(GraphQLRequest gqlRequest, IDictionary<string, object> userContext, IGraphQLExecuter<TSchema> executer, CancellationToken token)
            => executer.ExecuteAsync(
                gqlRequest.OperationName,
                gqlRequest.Query,
                gqlRequest.Inputs,
                userContext,
                token);

        protected virtual CancellationToken GetCancellationToken(HttpContext context) => context.RequestAborted;

        protected virtual Task RequestExecutedAsync(in GraphQLRequestExecutionResult requestExecutionResult)
        {
            // nothing to do in this middleware
            return Task.CompletedTask;
        }

        private Task WriteErrorResponseAsync(HttpResponse httpResponse, IDocumentWriter writer, CancellationToken cancellationToken,
            string errorMessage, int httpStatusCode = 400 /* BadRequest */)
        {
            var result = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError(errorMessage)
                }
            };

            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = httpStatusCode;

            return writer.WriteAsync(httpResponse.Body, result, cancellationToken);
        }

        private Task WriteResponseAsync<TResult>(HttpResponse httpResponse, IDocumentWriter writer, CancellationToken cancellationToken, TResult result)
        {
            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = 200; // OK

            return writer.WriteAsync(httpResponse.Body, result, cancellationToken);
        }

        private GraphQLRequest DeserializeFromQueryString(IQueryCollection queryCollection) => new GraphQLRequest
        {
            Query = queryCollection.TryGetValue(GraphQLRequest.QUERY_KEY, out var queryValues) ? queryValues[0] : null,
            Inputs = queryCollection.TryGetValue(GraphQLRequest.VARIABLES_KEY, out var variablesValues) ? _deserializer.DeserializeInputsFromJson(variablesValues[0]) : null,
            OperationName = queryCollection.TryGetValue(GraphQLRequest.OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null
        };

        private GraphQLRequest DeserializeFromFormBody(IFormCollection formCollection) => new GraphQLRequest
        {
            Query = formCollection.TryGetValue(GraphQLRequest.QUERY_KEY, out var queryValues) ? queryValues[0] : null,
            Inputs = formCollection.TryGetValue(GraphQLRequest.VARIABLES_KEY, out var variablesValue) ? _deserializer.DeserializeInputsFromJson(variablesValue[0]) : null,
            OperationName = formCollection.TryGetValue(GraphQLRequest.OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null
        };

        private async Task<GraphQLRequest> DeserializeFromGraphBodyAsync(Stream bodyStream)
        {
            // In this case, the query is the raw value in the POST body

            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            string query = await new StreamReader(bodyStream).ReadToEndAsync();

            return new GraphQLRequest { Query = query };
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Instrumentation;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// ASP.NET Core middleware for processing GraphQL requests. Can processes both single and batch requests.
    /// See <see href="https://www.apollographql.com/blog/query-batching-in-apollo-63acfd859862/">Transport-level batching</see>
    /// for more information. This middleware useful with and without ASP.NET Core routing.
    /// <br/><br/>
    /// GraphQL over HTTP <see href="https://github.com/APIs-guru/graphql-over-http">spec</see> says:
    /// GET requests can be used for executing ONLY queries. If the values of query and operationName indicates that
    /// a non-query operation is to be executed, the server should immediately respond with an error status code, and
    /// halt execution.
    /// <br/><br/>
    /// Attention! The current implementation does not impose such a restriction and allows mutations in GET requests.
    /// </summary>
    /// <typeparam name="TSchema">Type of GraphQL schema that is used to validate and process requests.</typeparam>
    public class GraphQLHttpMiddleware<TSchema> : IMiddleware
        where TSchema : ISchema
    {
        private const string DOCS_URL = "See: http://graphql.org/learn/serving-over-http/.";

        private readonly IGraphQLTextSerializer _serializer;

        public GraphQLHttpMiddleware(IGraphQLTextSerializer serializer)
        {
            _serializer = serializer;
        }

        public virtual async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await next(context);
                return;
            }

            // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
            // Inspiration: https://github.com/graphql/express-graphql/blob/master/src/index.js
            var httpRequest = context.Request;
            var httpResponse = context.Response;

            var cancellationToken = GetCancellationToken(context);

            // GraphQL HTTP only supports GET and POST methods
            bool isGet = HttpMethods.IsGet(httpRequest.Method);
            bool isPost = HttpMethods.IsPost(httpRequest.Method);
            if (!isGet && !isPost)
            {
                httpResponse.Headers["Allow"] = "GET, POST";
                await HandleInvalidHttpMethodErrorAsync(context);
                return;
            }

            // Parse POST body
            GraphQLRequest bodyGQLRequest = null;
            GraphQLRequest[] bodyGQLBatchRequest = null;
            if (isPost)
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
                {
                    await HandleContentTypeCouldNotBeParsedErrorAsync(context);
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case MediaType.JSON:
                        GraphQLRequest[] deserializationResult;
                        try
                        {
#if NET5_0_OR_GREATER
                            if (!TryGetEncoding(mediaTypeHeader.CharSet, out var sourceEncoding))
                            {
                                await HandleContentTypeCouldNotBeParsedErrorAsync(context);
                                return;
                            }
                            // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
                            if (sourceEncoding != null && sourceEncoding != System.Text.Encoding.UTF8)
                            {
                                using var tempStream = System.Text.Encoding.CreateTranscodingStream(httpRequest.Body, innerStreamEncoding: sourceEncoding, outerStreamEncoding: System.Text.Encoding.UTF8, leaveOpen: true);
                                deserializationResult = await _serializer.ReadAsync<GraphQLRequest[]>(tempStream, cancellationToken);
                            }
                            else
                            {
                                deserializationResult = await _serializer.ReadAsync<GraphQLRequest[]>(httpRequest.Body, cancellationToken);
                            }
#else
                            deserializationResult = await _serializer.ReadAsync<GraphQLRequest[]>(httpRequest.Body, cancellationToken);
#endif
                        }
                        catch (Exception ex)
                        {
                            if (!await HandleDeserializationErrorAsync(context, ex))
                                throw;
                            return;
                        }
                        if (deserializationResult?.Length == 1)
                            bodyGQLRequest = deserializationResult[0];
                        else
                            bodyGQLBatchRequest = deserializationResult;
                        break;

                    case MediaType.GRAPH_QL:
                        bodyGQLRequest = await DeserializeFromGraphBodyAsync(httpRequest.Body);
                        break;

                    default:
                        if (httpRequest.HasFormContentType)
                        {
                            var formCollection = await httpRequest.ReadFormAsync(cancellationToken);
                            try
                            {
                                bodyGQLRequest = DeserializeFromFormBody(formCollection);
                            }
                            catch (Exception ex)
                            {
                                if (!await HandleDeserializationErrorAsync(context, ex))
                                    throw;
                            }
                            break;
                        }
                        await HandleInvalidContentTypeErrorAsync(context);
                        return;
                }
            }

            // If we don't have a batch request, parse the query from URL too to determine the actual request to run.
            // Query string params take priority.
            GraphQLRequest gqlRequest = null;
            if (bodyGQLBatchRequest == null)
            {
                var urlGQLRequest = DeserializeFromQueryString(httpRequest.Query);

                gqlRequest = new GraphQLRequest
                {
                    Query = urlGQLRequest.Query ?? bodyGQLRequest?.Query,
                    Variables = urlGQLRequest.Variables ?? bodyGQLRequest?.Variables,
                    Extensions = urlGQLRequest.Extensions ?? bodyGQLRequest?.Extensions,
                    OperationName = urlGQLRequest.OperationName ?? bodyGQLRequest?.OperationName
                };

                if (string.IsNullOrWhiteSpace(gqlRequest.Query))
                {
                    await HandleNoQueryErrorAsync(context);
                    return;
                }
            }

            // Prepare context and execute
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
            var userContext = userContextBuilder == null
                ? new Dictionary<string, object>() // in order to allow resolvers to exchange their state through this object
                : await userContextBuilder.BuildUserContext(context);

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();
            await HandleRequestAsync(context, next, userContext, bodyGQLBatchRequest, gqlRequest, executer, cancellationToken);
        }

        protected virtual async Task HandleRequestAsync(
            HttpContext context,
            RequestDelegate next,
            IDictionary<string, object> userContext,
            GraphQLRequest[] bodyGQLBatchRequest,
            GraphQLRequest gqlRequest,
            IGraphQLExecuter<TSchema> executer,
            CancellationToken cancellationToken)
        {
            // Normal execution with single graphql request
            if (bodyGQLBatchRequest == null)
            {
                var stopwatch = ValueStopwatch.StartNew();
                await RequestExecutingAsync(gqlRequest);
                var result = await ExecuteRequestAsync(gqlRequest, userContext, executer, context.RequestServices, cancellationToken);

                await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequest, result, stopwatch.Elapsed));

                await WriteResponseAsync(context.Response, _serializer, cancellationToken, result);
            }
            // Execute multiple graphql requests in one batch
            else
            {
                var executionResults = new ExecutionResult[bodyGQLBatchRequest.Length];
                for (int i = 0; i < bodyGQLBatchRequest.Length; ++i)
                {
                    var gqlRequestInBatch = bodyGQLBatchRequest[i];

                    var stopwatch = ValueStopwatch.StartNew();
                    await RequestExecutingAsync(gqlRequestInBatch, i);
                    var result = await ExecuteRequestAsync(gqlRequestInBatch, userContext, executer, context.RequestServices, cancellationToken);

                    await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequestInBatch, result, stopwatch.Elapsed, i));

                    executionResults[i] = result;
                }

                await WriteResponseAsync(context.Response, _serializer, cancellationToken, executionResults);
            }
        }

        protected virtual async ValueTask<bool> HandleDeserializationErrorAsync(HttpContext context, Exception ex)
        {
            await WriteErrorResponseAsync(context, $"JSON body text could not be parsed. {ex.Message}", HttpStatusCode.BadRequest);
            return true;
        }

        protected virtual Task HandleNoQueryErrorAsync(HttpContext context)
            => WriteErrorResponseAsync(context, "GraphQL query is missing.", HttpStatusCode.BadRequest);

        protected virtual Task HandleContentTypeCouldNotBeParsedErrorAsync(HttpContext context)
            => WriteErrorResponseAsync(context, $"Invalid 'Content-Type' header: value '{context.Request.ContentType}' could not be parsed.", HttpStatusCode.UnsupportedMediaType);

        protected virtual Task HandleInvalidContentTypeErrorAsync(HttpContext context)
            => WriteErrorResponseAsync(context, $"Invalid 'Content-Type' header: non-supported media type '{context.Request.ContentType}'. Must be of '{MediaType.JSON}', '{MediaType.GRAPH_QL}' or '{MediaType.FORM}'. {DOCS_URL}", HttpStatusCode.UnsupportedMediaType);

        protected virtual Task HandleInvalidHttpMethodErrorAsync(HttpContext context)
            => WriteErrorResponseAsync(context, $"Invalid HTTP method. Only GET and POST are supported. {DOCS_URL}", HttpStatusCode.MethodNotAllowed);

        protected virtual Task<ExecutionResult> ExecuteRequestAsync(GraphQLRequest gqlRequest, IDictionary<string, object> userContext, IGraphQLExecuter<TSchema> executer, IServiceProvider requestServices, CancellationToken token)
            => executer.ExecuteAsync(
                gqlRequest,
                userContext,
                requestServices,
                token);

        protected virtual CancellationToken GetCancellationToken(HttpContext context) => context.RequestAborted;

        protected virtual Task RequestExecutingAsync(GraphQLRequest request, int? indexInBatch = null)
        {
            // nothing to do in this middleware
            return Task.CompletedTask;
        }

        protected virtual Task RequestExecutedAsync(in GraphQLRequestExecutionResult requestExecutionResult)
        {
            // nothing to do in this middleware
            return Task.CompletedTask;
        }

        protected virtual Task WriteErrorResponseAsync(HttpContext context, string errorMessage, HttpStatusCode httpStatusCode)
        {
            var result = new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError(errorMessage)
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)httpStatusCode;

            return _serializer.WriteAsync(context.Response.Body, result, GetCancellationToken(context));
        }

        protected virtual Task WriteResponseAsync<TResult>(HttpResponse httpResponse, IGraphQLSerializer serializer, CancellationToken cancellationToken, TResult result)
        {
            httpResponse.ContentType = "application/json";
            httpResponse.StatusCode = 200; // OK

            return serializer.WriteAsync(httpResponse.Body, result, cancellationToken);
        }

        private const string QUERY_KEY = "query";
        private const string VARIABLES_KEY = "variables";
        private const string EXTENSIONS_KEY = "extensions";
        private const string OPERATION_NAME_KEY = "operationName";

        private GraphQLRequest DeserializeFromQueryString(IQueryCollection queryCollection) => new GraphQLRequest
        {
            Query = queryCollection.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
            Variables = queryCollection.TryGetValue(VARIABLES_KEY, out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
            Extensions = queryCollection.TryGetValue(EXTENSIONS_KEY, out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
            OperationName = queryCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null
        };

        private GraphQLRequest DeserializeFromFormBody(IFormCollection formCollection) => new GraphQLRequest
        {
            Query = formCollection.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
            Variables = formCollection.TryGetValue(VARIABLES_KEY, out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
            Extensions = formCollection.TryGetValue(EXTENSIONS_KEY, out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
            OperationName = formCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null
        };

        private async Task<GraphQLRequest> DeserializeFromGraphBodyAsync(Stream bodyStream)
        {
            // In this case, the query is the raw value in the POST body

            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            string query = await new StreamReader(bodyStream).ReadToEndAsync();

            return new GraphQLRequest { Query = query }; // application/graphql MediaType supports only query text
        }

#if NET5_0_OR_GREATER
        private static bool TryGetEncoding(string charset, out System.Text.Encoding encoding)
        {
            encoding = null;

            if (string.IsNullOrEmpty(charset))
                return true;

            try
            {
                // Remove at most a single set of quotes.
                if (charset.Length > 2 && charset[0] == '\"' && charset[charset.Length - 1] == '\"')
                {
                    encoding = System.Text.Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                }
                else
                {
                    encoding = System.Text.Encoding.GetEncoding(charset);
                }
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }
#endif
    }
}

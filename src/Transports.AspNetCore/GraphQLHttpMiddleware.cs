using GraphQL.Http;
using GraphQL.Instrumentation;
using GraphQL.Server.Internal;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        private readonly JsonSerializer _serializer;

        public GraphQLHttpMiddleware(RequestDelegate next, PathString path, Action<JsonSerializerSettings> configure)
        {
            _next = next;
            _path = path;

            var settings = new JsonSerializerSettings();
            configure(settings);
            _serializer = JsonSerializer.Create(settings); // it's thread safe https://stackoverflow.com/questions/36186276/is-the-json-net-jsonserializer-threadsafe
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
            var gqlRequest = new GraphQLRequest();
            GraphQLRequest[] gqlBatchRequest = null;

            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();

            if (HttpMethods.IsGet(httpRequest.Method) || (HttpMethods.IsPost(httpRequest.Method) && httpRequest.Query.ContainsKey(GraphQLRequest.QueryKey)))
            {
                ExtractGraphQLRequestFromQueryString(httpRequest.Query, gqlRequest);
            }
            else if (HttpMethods.IsPost(httpRequest.Method))
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
                {
                    await WriteBadRequestResponseAsync(context, writer, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.").ConfigureAwait(false);
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case JsonContentType:
                        if (!Deserialize(httpRequest.Body, out gqlRequest, out gqlBatchRequest))
                        {
                            await WriteBadRequestResponseAsync(context, writer, "Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query.").ConfigureAwait(false);
                            return;
                        }
                        break;

                    case GraphQLContentType:
                        gqlRequest.Query = await ReadAsStringAsync(httpRequest.Body).ConfigureAwait(false);
                        break;

                    case FormUrlEncodedContentType:
                        var formCollection = await httpRequest.ReadFormAsync().ConfigureAwait(false);
                        ExtractGraphQLRequestFromPostBody(formCollection, gqlRequest);
                        break;

                    default:
                        await WriteBadRequestResponseAsync(context, writer, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{JsonContentType}', '{GraphQLContentType}', or '{FormUrlEncodedContentType}'. See: http://graphql.org/learn/serving-over-http/.").ConfigureAwait(false);
                        return;
                }
            }

            IDictionary<string, object> userContext = null;
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();

            if (userContextBuilder != null)
            {
                userContext = await userContextBuilder.BuildUserContext(context).ConfigureAwait(false);
            }
            else
                userContext = new Dictionary<string, object>(); // in order to allow resolvers to exchange their state through this object

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();
            var token = GetCancellationToken(context);

            // normal execution with single graphql request
            if (gqlBatchRequest == null)
            {
                var stopwatch = ValueStopwatch.StartNew();
                var result = await executer.ExecuteAsync(
                    gqlRequest.OperationName,
                    gqlRequest.Query,
                    gqlRequest.GetInputs(),
                    userContext,
                    token).ConfigureAwait(false);

                await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequest, result, stopwatch.Elapsed));

                await WriteResponseAsync(context, writer, result).ConfigureAwait(false);
            }
            // execute multiple graphql requests in one batch
            else
            {
                var executionResults = new ExecutionResult[gqlBatchRequest.Length];
                for (int i = 0; i < gqlBatchRequest.Length; ++i)
                {
                    var request = gqlBatchRequest[i];

                    var stopwatch = ValueStopwatch.StartNew();
                    var result = await executer.ExecuteAsync(
                        request.OperationName,
                        request.Query,
                        request.GetInputs(),
                        userContext,
                        token).ConfigureAwait(false);

                    await RequestExecutedAsync(new GraphQLRequestExecutionResult(gqlRequest, result, stopwatch.Elapsed, i));

                    executionResults[i] = result;
                }

                await WriteResponseAsync(context, writer, executionResults).ConfigureAwait(false);
            }
        }

        protected virtual CancellationToken GetCancellationToken(HttpContext context) => context.RequestAborted;

        protected virtual Task RequestExecutedAsync(in GraphQLRequestExecutionResult requestExecutionResult)
        {
            // nothing to do in this middleware
            return Task.CompletedTask;
        }

        private Task WriteBadRequestResponseAsync(HttpContext context, IDocumentWriter writer, string errorMessage)
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

            return writer.WriteAsync(context.Response.Body, result);
        }

        private Task WriteResponseAsync<TResult>(HttpContext context, IDocumentWriter writer, TResult result)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200; // OK

            return writer.WriteAsync(context.Response.Body, result);
        }

        private bool Deserialize(Stream stream, out GraphQLRequest single, out GraphQLRequest[] batch)
        {
            single = null;
            batch = null;

            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            var reader = new StreamReader(stream);
            using (var jsonReader = new JsonTextReader(reader) { CloseInput = false })
            {
                switch (reader.Peek())
                {
                    case '{':
                        single = _serializer.Deserialize<GraphQLRequest>(jsonReader);
                        return true;

                    case '[':
                        batch = _serializer.Deserialize<GraphQLRequest[]>(jsonReader);
                        return true;

                    default:
                        return false; // fast return with BadRequest without reading request stream
                }
            }
        }

        private static async Task<string> ReadAsStringAsync(Stream s)
        {
            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            return await new StreamReader(s).ReadToEndAsync().ConfigureAwait(false);
        }

        private static void ExtractGraphQLRequestFromQueryString(IQueryCollection qs, GraphQLRequest gqlRequest)
        {
            gqlRequest.Query = qs.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null;
            gqlRequest.Variables = qs.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValues) ? JObject.Parse(variablesValues[0]) : null;
            gqlRequest.OperationName = qs.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null;
        }

        private static void ExtractGraphQLRequestFromPostBody(IFormCollection fc, GraphQLRequest gqlRequest)
        {
            gqlRequest.Query = fc.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null;
            gqlRequest.Variables = fc.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValue) ? JObject.Parse(variablesValue[0]) : null;
            gqlRequest.OperationName = fc.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null;
        }
    }
}

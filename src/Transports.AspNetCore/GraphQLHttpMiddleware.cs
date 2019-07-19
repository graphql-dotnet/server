using GraphQL.Http;
using GraphQL.Server.Internal;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpMiddleware<TSchema>
        where TSchema : ISchema
    {
        private const string JsonContentType = "application/json";
        private const string GraphQLContentType = "application/graphql";
        private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";

        private readonly ILogger _logger;
        private readonly RequestDelegate _next;
        private readonly PathString _path;

        public GraphQLHttpMiddleware(ILogger<GraphQLHttpMiddleware<TSchema>> logger, RequestDelegate next, PathString path)
        {
            _logger = logger;
            _next = next;
            _path = path;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest || !context.Request.Path.StartsWithSegments(_path))
            {
                await _next(context);
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
                    await WriteBadRequestResponseAsync(context, writer, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.");
                    return;
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case JsonContentType:
                        // batching is supported only for POST and application/json
                        if (IsBatchRequest(httpRequest))
                            gqlBatchRequest = Deserialize<GraphQLRequest[]>(httpRequest.Body);
                        else
                            gqlRequest = Deserialize<GraphQLRequest>(httpRequest.Body);
                        break;
                    case GraphQLContentType:
                        gqlRequest.Query = await ReadAsStringAsync(httpRequest.Body);
                        break;
                    case FormUrlEncodedContentType:
                        var formCollection = await httpRequest.ReadFormAsync();
                        ExtractGraphQLRequestFromPostBody(formCollection, gqlRequest);
                        break;
                    default:
                        await WriteBadRequestResponseAsync(context, writer, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{JsonContentType}', '{GraphQLContentType}', or '{FormUrlEncodedContentType}'. See: http://graphql.org/learn/serving-over-http/.");
                        return;
                }
            }

            IDictionary<string, object> userContext = null;
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();

            if (userContextBuilder != null)
            {
                userContext = await userContextBuilder.BuildUserContext(context);
            }

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();

            // normal execution
            if (gqlBatchRequest == null)
            {
                var result = await executer.ExecuteAsync(
                    gqlRequest.OperationName,
                    gqlRequest.Query,
                    gqlRequest.GetInputs(),
                    userContext,
                    context.RequestAborted);

                if (result.Errors != null)
                {
                    _logger.LogError("GraphQL execution error(s): {Errors}", result.Errors);
                }

                await WriteResponseAsync(context, writer, result);
            }
            // execute multiple graphql requests in one batch
            else
            {
                var executionResults = new ExecutionResult[gqlBatchRequest.Length];
                for (int i = 0; i < gqlBatchRequest.Length; ++i)
                {
                    var request = gqlBatchRequest[i];

                    var result = await executer.ExecuteAsync(
                        request.OperationName,
                        request.Query,
                        request.GetInputs(),
                        userContext,
                        context.RequestAborted);

                    if (result.Errors != null)
                    {
                        _logger.LogError("GraphQL execution error(s) in batch [{Index}]: {Errors}", i, result.Errors);
                    }

                    executionResults[i] = result;
                }

                await WriteResponseAsync(context, writer, executionResults);
            }
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

        private static T Deserialize<T>(Stream s)
        {
            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            var reader = new StreamReader(s);
            using (var jsonReader = new JsonTextReader(reader) { CloseInput = false })
            {
                return new JsonSerializer().Deserialize<T>(jsonReader);
            }
        }

        private static async Task<string> ReadAsStringAsync(Stream s)
        {
            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            return await new StreamReader(s).ReadToEndAsync();
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

        private static bool IsBatchRequest(HttpRequest request)
        {
            // Generally speaking it would be possible to determine a batch query by the presence of an opening square bracket at the beginning
            // of the request body, but this is fraught with errors and possible problems associated with buffering the request stream. It is
            // better to explicitly check the presence of the special header - it is safer and easier.
            return request.Headers["graphql-batch"] == "true";
        }
    }
}

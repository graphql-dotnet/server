using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Internal;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpMiddleware<TSchema>
        where TSchema : ISchema
    {
        private const string JsonContentType = "application/json";
        private const string GraphQLContentType = "application/graphql";

        private readonly RequestDelegate _next;
        private readonly PathString _path;

        public GraphQLHttpMiddleware(RequestDelegate next, PathString path)
        {
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

            var writer = context.RequestServices.GetRequiredService<IDocumentWriter>();

            if (HttpMethods.IsGet(httpRequest.Method) || (HttpMethods.IsPost(httpRequest.Method) && httpRequest.Query.ContainsKey(GraphQLRequest.QueryKey)))
            {
                ExtractGraphQLRequestFromQueryString(httpRequest.Query, gqlRequest);
            }
            else if (HttpMethods.IsPost(httpRequest.Method))
            {
                if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
                {
                    await WriteErrorResponseAsync(context, writer, $"Invalid 'Content-Type' header: value '{httpRequest.ContentType}' could not be parsed.");
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
                        await WriteErrorResponseAsync(context, writer, $"Invalid 'Content-Type' header: non-supported media type. Must be of '{JsonContentType}' or '{GraphQLContentType}'. See: http://graphql.org/learn/serving-over-http/.");
                        return;
                }
            }

            object userContext = null;
            var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();

            if (userContextBuilder != null)
            {
                userContext = await userContextBuilder.BuildUserContext(context);
            }

            var executer = context.RequestServices.GetRequiredService<IGraphQLExecuter<TSchema>>();

            var result = await executer.ExecuteAsync(
                gqlRequest.OperationName,
                gqlRequest.Query,
                gqlRequest.GetInputs(),
                userContext,
                context.RequestAborted);

            await WriteResponseAsync(context, writer, result);
        }

        private Task WriteErrorResponseAsync(HttpContext context, IDocumentWriter writer, string errorMessage)
        {
            var result = new ExecutionResult()
            {
                Errors = new ExecutionErrors()
                {
                    new ExecutionError(errorMessage)
                }
            };

            return WriteResponseAsync(context, writer, result);
        }

        private Task WriteResponseAsync(HttpContext context, IDocumentWriter writer, ExecutionResult result)
        {
            var json = writer.Write(result);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = result.Errors?.Any() == true ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.OK;

            return context.Response.WriteAsync(json);
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
            gqlRequest.Query = qs.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null;
            gqlRequest.Variables = qs.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValues) ? JObject.Parse(variablesValues[0]) : null;
            gqlRequest.OperationName = qs.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null;
        }
    }
}

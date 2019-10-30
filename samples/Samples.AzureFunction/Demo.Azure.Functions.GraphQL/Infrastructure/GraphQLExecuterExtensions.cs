using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using GraphQL;
using GraphQL.Server.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Demo.Azure.Functions.GraphQL.Infrastructure
{
    internal static class GraphQLExecuterExtensions
    {
        private const string OPERATION_NAME_KEY = "operationName";
        private const string QUERY_KEY = "query";
        private const string VARIABLES_KEY = "variables";

        private const string JSON_MEDIA_TYPE = "application/json";
        private const string GRAPHQL_MEDIA_TYPE = "application/graphql";
        private const string FORM_URLENCODED_MEDIA_TYPE = "application/x-www-form-urlencoded";

        public async static Task<ExecutionResult> ExecuteAsync(this IGraphQLExecuter graphQLExecuter, HttpRequest request)
        {
            string operationName = null;
            string query = null;
            JObject variables = null;

            if (HttpMethods.IsGet(request.Method) || (HttpMethods.IsPost(request.Method) && request.Query.ContainsKey(QUERY_KEY)))
            {
                (operationName, query, variables) = ExtractGraphQLAttributesFromQueryString(request);
            }
            else if (HttpMethods.IsPost(request.Method))
            {
                if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader))
                {
                    throw new GraphQLBadRequestException($"Could not parse 'Content-Type' header value '{request.ContentType}'.");
                }

                switch (mediaTypeHeader.MediaType)
                {
                    case JSON_MEDIA_TYPE:
                        (operationName, query, variables) = await ExtractGraphQLAttributesFromJsonBodyAsync(request);
                        break;
                    case GRAPHQL_MEDIA_TYPE:
                        query = await ExtractGraphQLQueryFromGraphQLBodyAsync(request.Body);
                        break;
                    case FORM_URLENCODED_MEDIA_TYPE:
                        (operationName, query, variables) = await ExtractGraphQLAttributesFromFormCollectionAsync(request);
                        break;
                    default:
                        throw new GraphQLBadRequestException($"Not supported 'Content-Type' header value '{request.ContentType}'.");
                }
            }
            
            return await graphQLExecuter.ExecuteAsync(operationName, query, variables?.ToInputs(), null, request.HttpContext.RequestAborted);
        }

        private static (string operationName, string query, JObject variables) ExtractGraphQLAttributesFromQueryString(HttpRequest request)
        {
            return (
                request.Query.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null,
                request.Query.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
                request.Query.TryGetValue(VARIABLES_KEY, out var variablesValues) ? JObject.Parse(variablesValues[0]) : null
            );
        }

        private async static Task<(string operationName, string query, JObject variables)> ExtractGraphQLAttributesFromJsonBodyAsync(HttpRequest request)
        {
            using (StreamReader bodyReader = new StreamReader(request.Body))
            {
                using (JsonTextReader bodyJsonReader = new JsonTextReader(bodyReader))
                {
                    JObject bodyJson = await JObject.LoadAsync(bodyJsonReader);

                    return (
                        bodyJson.Value<String>(OPERATION_NAME_KEY),
                        bodyJson.Value<String>(QUERY_KEY),
                        bodyJson.Value<JObject>(VARIABLES_KEY)
                    );
                }
            }
        }

        private static Task<string> ExtractGraphQLQueryFromGraphQLBodyAsync(Stream body)
        {
            using (StreamReader bodyReader = new StreamReader(body))
            {
                return bodyReader.ReadToEndAsync();
            }
        }

        private async static Task<(string operationName, string query, JObject variables)> ExtractGraphQLAttributesFromFormCollectionAsync(HttpRequest request)
        {
            IFormCollection requestFormCollection = await request.ReadFormAsync();

            return (
                requestFormCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null,
                requestFormCollection.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
                requestFormCollection.TryGetValue(VARIABLES_KEY, out var variablesValue) ? JObject.Parse(variablesValue[0]) : null
                );
        }
    }
}

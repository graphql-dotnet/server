using System.Text;
using System.Threading.Tasks;
using GraphQL.Common.Response;
using Newtonsoft.Json;

namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static async Task<GraphQLResponse> QueryAsync(this HttpClient httpClient, string query, object variables = null, string operationName = null, string endpoint = "/graphql")
        {
            string json = JsonConvert.SerializeObject(new { query, variables, operationName });

            var response = await httpClient.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<GraphQLResponse>(responseJson);
        }
    }
}

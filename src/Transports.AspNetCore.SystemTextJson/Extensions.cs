using System.Text.Json;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    public static class Extensions
    {
        public static JsonElement ToVariables(this string json)
        {
            using var jsonDoc = JsonDocument.Parse(json);
            return jsonDoc.RootElement.Clone();
        }
    }
}

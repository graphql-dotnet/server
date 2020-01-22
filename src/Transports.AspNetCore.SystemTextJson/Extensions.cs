using System.Text.Json;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    public static class Extensions
    {
        public static JsonDocument ToVariables(this string json) => JsonDocument.Parse(json);
    }
}

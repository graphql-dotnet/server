using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    public static class Extensions
    {
        public static JObject ToVariables(this string json) => JObject.Parse(json);
    }
}

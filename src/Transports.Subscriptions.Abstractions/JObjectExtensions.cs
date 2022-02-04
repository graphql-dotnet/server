using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    internal static class JObjectExtensions
    {
        public static Inputs ToInputs(this JObject jObject)
            => new NewtonsoftJson.GraphQLSerializer().ReadNode<Inputs>(jObject);
    }
}

using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Voyager
{
    public class GraphQLVoyagerOptions
    {
        public PathString Path { get; set; } = "/ui/voyager";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";
    }
}

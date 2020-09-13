using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Voyager
{
    /// <summary>
    /// Options to customize <see cref="VoyagerMiddleware"/>
    /// </summary>
    public class GraphQLVoyagerOptions
    {
        /// <summary>
        /// The Voyager EndPoint to listen
        /// </summary>
        public PathString Path { get; set; } = "/ui/voyager";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// HTTP Headers with which the Voyager will send introspection query
        /// </summary>
        public Dictionary<string, object> Headers { get; set; }
    }
}

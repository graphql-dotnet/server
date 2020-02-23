using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace GraphQL.Server.Ui.Playground
{
    /// <summary>
    /// Options to customize <see cref="PlaygroundMiddleware"/>
    /// </summary>
    public class GraphQLPlaygroundOptions
    {
        /// <summary>
        /// The Playground EndPoint to listen
        /// </summary>
        public PathString Path { get; set; } = "/ui/playground";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// The GraphQL Config
        /// </summary>
        public Dictionary<string, object> GraphQLConfig { get; set; }

        /// <summary>
        /// The GraphQL Playground Settings
        /// </summary>
        public Dictionary<string, object> PlaygroundSettings { get; set; }

        /// <summary>
        /// HTTP Headers with which the GraphQL Playground will be initialized
        /// </summary>
        public Dictionary<string, object> Headers { get; set; }
    }
}

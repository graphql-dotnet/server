using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Playground {

    public class GraphQLPlaygroundOptions {

        public PathString Path { get; set; } = "/ui/playground";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// The GraphQL Config (as JSON string)
        /// </summary>
        public string GraphQLConfig { get; set; } = null;

        /// <summary>
        /// The GraphQL Playground Settings (as JSON string)
        /// </summary>
        public string PlaygroundSettings { get; set; } = null;

    }

}

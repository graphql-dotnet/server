using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.GraphiQL
{
    /// <summary>
    /// Options to customize the <see cref="GraphiQLMiddleware"/>
    /// </summary>
    public class GraphiQLOptions
    {
        /// <summary>
        /// The GraphiQL Endpoint to listen
        /// </summary>
        public PathString Path { get; set; } = "/ui/graphiql";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";
    }
}

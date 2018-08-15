using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.GraphiQL
{
    /// <summary>
    /// The settings of the <see cref="GraphiQLMiddleware"/>
    /// </summary>
    public class GraphiQLOptions
    {
        /// <summary>
        /// The GraphiQL Endpoint to listen
        /// </summary>
        public PathString GraphiQLPath { get; set; } = "/graphiql";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";
    }
}

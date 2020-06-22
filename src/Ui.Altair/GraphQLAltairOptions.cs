using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Altair
{
    /// <summary>
    /// Options to customize <see cref="AltairMiddleware"/>
    /// </summary>
    public class GraphQLAltairOptions
    {
        /// <summary>
        /// The Altair GraphQL EndPoint to listen
        /// </summary>
        public PathString Path { get; set; } = "/ui/altair";

        /// <summary>
        /// The GraphQL EndPoint
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// Altair Headers Config
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
    }
}

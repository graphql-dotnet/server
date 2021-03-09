using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Voyager
{
    /// <summary>
    /// Options to customize <see cref="VoyagerMiddleware"/>.
    /// </summary>
    public class VoyagerOptions
    {
        /// <summary>
        /// The GraphQL EndPoint.
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// HTTP headers with which the Voyager will send introspection query.
        /// </summary>
        public Dictionary<string, object> Headers { get; set; }
    }
}

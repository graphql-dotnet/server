using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Altair
{
    /// <summary>
    /// Options to customize <see cref="AltairMiddleware"/>.
    /// </summary>
    public class AltairOptions
    {
        /// <summary>
        /// The GraphQL EndPoint.
        /// </summary>
        public PathString GraphQLEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// Subscriptions EndPoint.
        /// </summary>
        public PathString SubscriptionsEndPoint { get; set; } = "/graphql";

        /// <summary>
        /// Altair headers configuration.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
    }
}

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.GraphiQL
{
    /// <summary>
    /// Options to customize the <see cref="GraphiQLMiddleware"/>.
    /// </summary>
    public class GraphiQLOptions
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
        /// HTTP headers with which the GraphiQL will be initialized.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Enables the header editor when <c>true</c>.
        /// Not supported when ExplorerExtensionEnabled is <c>true</c>.
        /// </summary>
        public bool HeaderEditorEnabled { get; set; } = true;

        /// <summary>
        /// Enables the explorer extension when <c>true</c>.
        /// </summary>
        public bool ExplorerExtensionEnabled { get; set; } = true;
    }
}

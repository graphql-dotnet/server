using System;
using System.Collections.Generic;
using System.IO;
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
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Gets or sets a Stream function for retrieving the GraphiQL UI page.
        /// </summary>
        public Func<GraphiQLOptions, Stream> IndexStream { get; set; } = _ => typeof(GraphiQLOptions).Assembly
            .GetManifestResourceStream("GraphQL.Server.Ui.GraphiQL.Internal.graphiql.cshtml")!;

        /// <summary>
        /// Gets or sets a delegate that is called after all transformations of the GraphiQL UI page.
        /// </summary>
        public Func<GraphiQLOptions, string, string> PostConfigure { get; set; } = (options, result) => result;
    }
}

using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.GraphiQL;

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

    /// <summary>
    /// Enables the header editor when <see langword="true"/>.
    /// Not supported when <see cref="ExplorerExtensionEnabled"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Original setting from <see href="https://github.com/graphql/graphiql/blob/08250feb6ee8335c3b1ca83a912911ae92a75722/packages/graphiql/src/components/GraphiQL.tsx#L186">GraphiQL</see>.
    /// </remarks>
    public bool HeaderEditorEnabled { get; set; } = true;

    /// <summary>
    /// Enables the explorer extension when <see langword="true"/>.
    /// </summary>
    public bool ExplorerExtensionEnabled { get; set; } = true;
}

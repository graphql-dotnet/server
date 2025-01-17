namespace GraphQL.Server.Ui.GraphiQL;

/// <summary>
/// Options to customize the <see cref="GraphiQLMiddleware"/>.
/// </summary>
public class GraphiQLOptions
{
    /// <summary>
    /// The GraphQL EndPoint.
    /// </summary>
    public string GraphQLEndPoint { get; set; } = "/graphql";

    /// <summary>
    /// Subscriptions EndPoint.
    /// </summary>
    public string SubscriptionsEndPoint { get; set; } = "/graphql";

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
    /// </summary>
    /// <remarks>
    /// Original setting from <see href="https://github.com/graphql/graphiql/blob/08250feb6ee8335c3b1ca83a912911ae92a75722/packages/graphiql/src/components/GraphiQL.tsx#L186">GraphiQL</see>.
    /// </remarks>
    public bool HeaderEditorEnabled { get; set; } = true;

    /// <summary>
    /// This property has no effect.
    /// </summary>
    [Obsolete("This property has no effect and will be removed in a future version.")]
    public bool ExplorerExtensionEnabled { get; set; } = true;

    /// <summary>
    /// Indicates whether the user agent should send cookies from the other domain
    /// in the case of cross-origin requests.
    /// </summary>
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials"/>.
    /// </remarks>
    public RequestCredentials RequestCredentials { get; set; } = RequestCredentials.SameOrigin;

    /// <summary>
    /// Use the graphql-ws package instead of the subscription-transports-ws package for subscriptions.
    /// </summary>
    public bool GraphQLWsSubscriptions { get; set; }
}

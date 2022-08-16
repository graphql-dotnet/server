namespace GraphQL.Server.Ui.Playground;

/// <summary>
/// Options to customize <see cref="PlaygroundMiddleware"/>.
/// </summary>
public class PlaygroundOptions
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
    /// The GraphQL configuration.
    /// </summary>
    public Dictionary<string, object>? GraphQLConfig { get; set; }

    /// <summary>
    /// HTTP headers with which the GraphQL Playground will be initialized.
    /// </summary>
    public Dictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets a Stream function for retrieving the GraphQL Playground UI page.
    /// </summary>
    public Func<PlaygroundOptions, Stream> IndexStream { get; set; } = _ => typeof(PlaygroundOptions).Assembly
        .GetManifestResourceStream("GraphQL.Server.Ui.Playground.Internal.playground.cshtml")!;

    /// <summary>
    /// Gets or sets a delegate that is called after all transformations of the GraphQL Playground UI page.
    /// </summary>
    public Func<PlaygroundOptions, string, string> PostConfigure { get; set; } = (options, result) => result;

    /// <summary>
    /// The GraphQL Playground Settings, see <see href="https://github.com/prisma-labs/graphql-playground/blob/master/README.md#settings"/>.
    /// </summary>
    public Dictionary<string, object> PlaygroundSettings { get; set; } = new Dictionary<string, object>();

    /* typed settings below are just wrappers for PlaygroundSettings dictionary */

    /// <summary>
    /// Cursor shape.
    /// </summary>
    public EditorCursorShape EditorCursorShape
    {
        get => (EditorCursorShape)Enum.Parse(typeof(EditorCursorShape), (string)PlaygroundSettings["editor.cursorShape"], ignoreCase: true);
        set => PlaygroundSettings["editor.cursorShape"] = value.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Source Code Pro, Consolas, Inconsolata, Droid Sans Mono, Monaco, monospace.
    /// </summary>
    public string EditorFontFamily
    {
        get => (string)PlaygroundSettings["editor.fontFamily"];
        set => PlaygroundSettings["editor.fontFamily"] = value;
    }

    /// <summary>
    /// Editor font size.
    /// </summary>
    public int EditorFontSize
    {
        get => (int)PlaygroundSettings["editor.fontSize"];
        set => PlaygroundSettings["editor.fontSize"] = value;
    }

    /// <summary>
    /// New tab reuses headers from last tab.
    /// </summary>
    public bool EditorReuseHeaders
    {
        get => (bool)PlaygroundSettings["editor.reuseHeaders"];
        set => PlaygroundSettings["editor.reuseHeaders"] = value;
    }

    /// <summary>
    /// Editor theme.
    /// </summary>
    public EditorTheme EditorTheme
    {
        get => (EditorTheme)Enum.Parse(typeof(EditorTheme), (string)PlaygroundSettings["editor.theme"], ignoreCase: true);
        set => PlaygroundSettings["editor.theme"] = value.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Enable beta updates.
    /// </summary>
    public bool BetaUpdates
    {
        get => (bool)PlaygroundSettings["general.betaUpdates"];
        set => PlaygroundSettings["general.betaUpdates"] = value;
    }

    /// <summary>
    /// Print width setting.
    /// </summary>
    public int PrettierPrintWidth
    {
        get => (int)PlaygroundSettings["prettier.printWidth"];
        set => PlaygroundSettings["prettier.printWidth"] = value;
    }

    /// <summary>
    /// Tab width setting.
    /// </summary>
    public int PrettierTabWidth
    {
        get => (int)PlaygroundSettings["prettier.tabWidth"];
        set => PlaygroundSettings["prettier.tabWidth"] = value;
    }

    /// <summary>
    /// Use tabs.
    /// </summary>
    public bool PrettierUseTabs
    {
        get => (bool)PlaygroundSettings["prettier.useTabs"];
        set => PlaygroundSettings["prettier.useTabs"] = value;
    }

    /// <summary>
    /// Indicates whether the user agent should send cookies from the other domain
    /// in the case of cross-origin requests.
    /// </summary>
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials"/>.
    /// </remarks>
    public RequestCredentials RequestCredentials
    {
        get => (string)PlaygroundSettings["request.credentials"] switch
        {
            "omit" => RequestCredentials.Omit,
            "include" => RequestCredentials.Include,
            "same-origin" => RequestCredentials.SameOrigin,
            _ => throw new NotSupportedException()
        };
        set => PlaygroundSettings["request.credentials"] = value switch
        {
            RequestCredentials.Omit => "omit",
            RequestCredentials.Include => "include",
            RequestCredentials.SameOrigin => "same-origin",
            _ => throw new NotSupportedException()
        };
    }

    /// <summary>
    /// Enables automatic schema polling.
    /// </summary>
    public bool SchemaPollingEnabled
    {
        get => (bool)PlaygroundSettings["schema.polling.enable"];
        set => PlaygroundSettings["schema.polling.enable"] = value;
    }

    /// <summary>
    /// Endpoint filter for schema polling, for example *localhost*.
    /// </summary>
    public string SchemaPollingEndpointFilter
    {
        get => (string)PlaygroundSettings["schema.polling.endpointFilter"];
        set => PlaygroundSettings["schema.polling.endpointFilter"] = value;
    }

    /// <summary>
    /// Schema polling interval in ms.
    /// </summary>
    public int SchemaPollingInterval
    {
        get => (int)PlaygroundSettings["schema.polling.interval"];
        set => PlaygroundSettings["schema.polling.interval"] = value;
    }

    /// <summary>
    /// Disable comments.
    /// </summary>
    public bool SchemaDisableComments
    {
        get => (bool)PlaygroundSettings["schema.disableComments"];
        set => PlaygroundSettings["schema.disableComments"] = value;
    }

    /// <summary>
    /// Hide tracing data in responses.
    /// </summary>
    public bool HideTracingResponse
    {
        get => (bool)PlaygroundSettings["tracing.hideTracingResponse"];
        set => PlaygroundSettings["tracing.hideTracingResponse"] = value;
    }
}

/// <summary>
/// Available cursor shapes.
/// </summary>
public enum EditorCursorShape
{
    /// <summary>
    /// Line.
    /// </summary>
    Line,
    /// <summary>
    /// Block.
    /// </summary>
    Block,
    /// <summary>
    /// Underline.
    /// </summary>
    Underline
}

/// <summary>
/// Available editor themes.
/// </summary>
public enum EditorTheme
{
    /// <summary>
    /// Dark theme.
    /// </summary>
    Dark,
    /// <summary>
    /// Light theme.
    /// </summary>
    Light
}

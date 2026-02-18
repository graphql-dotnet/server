namespace GraphQL.Server.Ui.Altair;

/// <summary>
/// Options to customize <see cref="AltairMiddleware"/>.
/// </summary>
public class AltairOptions
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
    /// Altair headers configuration.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Subscriptions payload.
    /// </summary>
    public Dictionary<string, object?>? SubscriptionsPayload { get; set; }

    /// <summary>
    /// Altair UI settings.
    /// <a href="https://altairgraphql.dev/docs/features/settings-pane.html">Available settings</a>
    /// </summary>
    public Dictionary<string, object?>? Settings { get; set; }

    /// <summary>
    /// Gets or sets a Stream function for retrieving the Altair GraphQL UI page.
    /// </summary>
    public Func<AltairOptions, Stream> IndexStream { get; set; } = _ => typeof(AltairOptions).Assembly
        .GetManifestResourceStream("GraphQL.Server.Ui.Altair.Internal.altair.cshtml")!;

    /// <summary>
    /// Gets or sets a delegate that is called after all transformations of the Altair GraphQL UI page.
    /// </summary>
    public Func<AltairOptions, string, string> PostConfigure { get; set; } = (options, result) => result;

    /// <summary>
    /// Optional parameter to pin altair-static package version, e.g. "8.5.1".
    /// Will be used to access a specific cdn version e.g.: "https://cdn.jsdelivr.net/npm/altair-static@8.5.1/build/dist".
    /// If empty (default value), automatic version resolution is used where no version pinning is utilized (latest).
    /// </summary>
    public string AltairVersion { get; set; } = string.Empty;
}

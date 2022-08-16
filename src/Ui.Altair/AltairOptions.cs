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
    /// Gets or sets a Stream function for retrieving the Altair GraphQL UI page.
    /// </summary>
    public Func<AltairOptions, Stream> IndexStream { get; set; } = _ => typeof(AltairOptions).Assembly
        .GetManifestResourceStream("GraphQL.Server.Ui.Altair.Internal.altair.cshtml")!;

    /// <summary>
    /// Gets or sets a delegate that is called after all transformations of the Altair GraphQL UI page.
    /// </summary>
    public Func<AltairOptions, string, string> PostConfigure { get; set; } = (options, result) => result;
}

namespace GraphQL.Server.Ui.Voyager;

/// <summary>
/// Options to customize <see cref="VoyagerMiddleware"/>.
/// </summary>
public class VoyagerOptions
{
    /// <summary>
    /// The GraphQL EndPoint.
    /// </summary>
    public string GraphQLEndPoint { get; set; } = "/graphql";

    /// <summary>
    /// HTTP headers with which the Voyager will send introspection query.
    /// </summary>
    public Dictionary<string, object>? Headers { get; set; }

    /// <summary>
    /// Gets or sets a Stream function for retrieving the Voyager UI page.
    /// </summary>
    public Func<VoyagerOptions, Stream> IndexStream { get; set; } = _ => typeof(VoyagerOptions).Assembly
        .GetManifestResourceStream("GraphQL.Server.Ui.Voyager.Internal.voyager.cshtml")!;

    /// <summary>
    /// Gets or sets a delegate that is called after all transformations of the Voyager UI page.
    /// </summary>
    public Func<VoyagerOptions, string, string> PostConfigure { get; set; } = (options, result) => result;

    /// <summary>
    /// Indicates whether the user agent should send cookies from the other domain
    /// in the case of cross-origin requests.
    /// </summary>
    /// <remarks>
    /// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials"/>.
    /// </remarks>
    public RequestCredentials RequestCredentials { get; set; } = RequestCredentials.SameOrigin;
}

namespace GraphQL.Server.Ui.Playground;

/// <summary>
/// Indicates whether the user agent should send cookies from the other domain
/// in the case of cross-origin requests.
/// </summary>
public enum RequestCredentials
{
    /// <summary>
    /// Never send or receive cookies.
    /// </summary>
    Omit,

    /// <summary>
    /// Always send user credentials (cookies, basic http auth, etc..), even for cross-origin calls.
    /// </summary>
    Include,

    /// <summary>
    /// Send user credentials (cookies, basic http auth, etc..) if the URL is on the same origin as the calling script.
    /// </summary>
    SameOrigin
}

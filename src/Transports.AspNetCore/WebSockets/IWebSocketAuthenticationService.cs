namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Authenticates an incoming GraphQL over WebSockets request with the
/// connection initialization message.  A typical implementation will
/// set the <see cref="HttpContext.User"/> property after reading the
/// authorization token.  This service must be registered as a singleton
/// in the dependency injection framework.
/// </summary>
public interface IWebSocketAuthenticationService
{
    /// <summary>
    /// Authenticates an incoming GraphQL over WebSockets request with the connection initialization message.  The implementation should
    /// set the <paramref name="connection"/>.<see cref="IWebSocketConnection.HttpContext">HttpContext</see>.<see cref="HttpContext.User">User</see>
    /// property after validating the provided credentials.
    /// <br/><br/>
    /// After calling this method to authenticate the request, the infrastructure will authorize the incoming request via the
    /// <see cref="GraphQLHttpMiddlewareOptions.AuthorizationRequired"/>, <see cref="GraphQLHttpMiddlewareOptions.AuthorizedRoles"/> and
    /// <see cref="GraphQLHttpMiddlewareOptions.AuthorizedPolicy"/> properties.
    /// </summary>
    Task AuthenticateAsync(IWebSocketConnection connection, string subProtocol, OperationMessage operationMessage);
}

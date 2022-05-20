using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Creates a user context from a <see cref="HttpContext"/>.
/// <br/><br/>
/// The generated user context may be used for one or more GraphQL requests or
/// subscriptions over the same HTTP connection.
/// </summary>
public interface IUserContextBuilder
{
    /// <inheritdoc cref="IUserContextBuilder"/>
    ValueTask<IDictionary<string, object?>> BuildUserContextAsync(HttpContext context);
}

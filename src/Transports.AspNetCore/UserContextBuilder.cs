using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Represents a user context builder based on a delegate.
/// </summary>
public class UserContextBuilder<TUserContext> : IUserContextBuilder
    where TUserContext : IDictionary<string, object?>
{
    private readonly Func<HttpContext, ValueTask<TUserContext>> _func;

    /// <summary>
    /// Initializes a new instance with the specified delegate.
    /// </summary>
    public UserContextBuilder(Func<HttpContext, ValueTask<TUserContext>> func)
    {
        _func = func ?? throw new ArgumentNullException(nameof(func));
    }

    /// <summary>
    /// Initializes a new instance with the specified delegate.
    /// </summary>
    public UserContextBuilder(Func<HttpContext, TUserContext> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        _func = x => new(func(x));
    }

    /// <inheritdoc/>
    public async ValueTask<IDictionary<string, object?>> BuildUserContextAsync(HttpContext context)
        => await _func(context);
}

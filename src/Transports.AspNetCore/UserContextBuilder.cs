namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Represents a user context builder based on a delegate.
/// </summary>
public class UserContextBuilder<TUserContext> : IUserContextBuilder
    where TUserContext : IDictionary<string, object?>
{
    private readonly Func<HttpContext, object?, ValueTask<IDictionary<string, object?>?>> _func;

    /// <summary>
    /// Initializes a new instance with the specified delegate.
    /// </summary>
    public UserContextBuilder(Func<HttpContext, ValueTask<TUserContext>> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        _func = async (context, _) => await func(context);
    }

    /// <inheritdoc cref="UserContextBuilder(Func{HttpContext, ValueTask{TUserContext}})"/>
    public UserContextBuilder(Func<HttpContext, TUserContext> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        _func = (context, _) => new(func(context));
    }

    /// <inheritdoc cref="UserContextBuilder(Func{HttpContext, ValueTask{TUserContext}})"/>
    public UserContextBuilder(Func<HttpContext, object?, ValueTask<TUserContext>> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        if (func is Func<HttpContext, object?, ValueTask<IDictionary<string, object?>?>> func2)
        {
            _func = func2;
        }
        else
        {
            _func = async (context, payload) => await func(context, payload);
        }
    }

    /// <inheritdoc cref="UserContextBuilder(Func{HttpContext, ValueTask{TUserContext}})"/>
    public UserContextBuilder(Func<HttpContext, object?, TUserContext> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        _func = (context, payload) => new(func(context, payload));
    }

    /// <inheritdoc/>
    public ValueTask<IDictionary<string, object?>?> BuildUserContextAsync(HttpContext context, object? payload)
        => _func(context, payload);
}

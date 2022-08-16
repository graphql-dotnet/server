namespace GraphQL;

/// <summary>
/// Extension methods for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class ServerGraphQLBuilderExtensions
{
    /// <summary>
    /// Registers the specified <typeparamref name="TUserContextBuilder"/> as a singleton of
    /// type <see cref="IUserContextBuilder"/> and configures it to be used for each GraphQL request.
    /// <br/><br/>
    /// Requires <see cref="IHttpContextAccessor"/> to be registered within the dependency injection framework
    /// if calling <see cref="DocumentExecuter.ExecuteAsync(ExecutionOptions)"/> directly.
    /// </summary>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContextBuilder>(this IGraphQLBuilder builder)
        where TUserContextBuilder : class, IUserContextBuilder
    {
        builder.Services.Register<IUserContextBuilder, TUserContextBuilder>(DI.ServiceLifetime.Singleton);
        builder.Services.TryRegister<IConfigureExecution, UserContextConfigurator>(DI.ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);

        return builder;
    }

    /// <summary>
    /// Configures a delegate to be used to create a user context for each GraphQL request.
    /// <br/><br/>
    /// Requires <see cref="IHttpContextAccessor"/> to be registered within the dependency injection framework
    /// if calling <see cref="DocumentExecuter.ExecuteAsync(ExecutionOptions)"/> directly.
    /// </summary>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, TUserContext> creator)
        where TUserContext : class, IDictionary<string, object?>
    {
        builder.Services.Register<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator));
        builder.Services.TryRegister<IConfigureExecution, UserContextConfigurator>(DI.ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);

        return builder;
    }

    /// <inheritdoc cref="AddUserContextBuilder{TUserContext}(IGraphQLBuilder, Func{HttpContext, TUserContext})"/>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, Task<TUserContext>> creator)
        where TUserContext : class, IDictionary<string, object?>
    {
        if (creator == null)
            throw new ArgumentNullException(nameof(creator));
        builder.Services.Register<IUserContextBuilder>(new UserContextBuilder<TUserContext>(context => new(creator(context))));
        builder.Services.TryRegister<IConfigureExecution, UserContextConfigurator>(DI.ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);

        return builder;
    }

    /// <inheritdoc cref="AddUserContextBuilder{TUserContext}(IGraphQLBuilder, Func{HttpContext, TUserContext})"/>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, object?, TUserContext> creator)
        where TUserContext : class, IDictionary<string, object?>
    {
        builder.Services.Register<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator ?? throw new ArgumentNullException(nameof(creator))));
        builder.Services.TryRegister<IConfigureExecution, UserContextConfigurator>(DI.ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);

        return builder;
    }

    /// <inheritdoc cref="AddUserContextBuilder{TUserContext}(IGraphQLBuilder, Func{HttpContext, TUserContext})"/>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, object?, Task<TUserContext>> creator)
        where TUserContext : class, IDictionary<string, object?>
    {
        if (creator == null)
            throw new ArgumentNullException(nameof(creator));
        builder.Services.Register<IUserContextBuilder>(new UserContextBuilder<TUserContext>((context, payload) => new(creator(context, payload))));
        builder.Services.TryRegister<IConfigureExecution, UserContextConfigurator>(DI.ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);

        return builder;
    }

    /// <summary>
    /// Checks if <see cref="ExecutionOptions.UserContext"/> is still at its initial "default" value, and if so,
    /// creates the user context from the registered <see cref="IUserContextBuilder"/>.
    /// <br/><br/>
    /// Typically the middleware initializes the user context, which is specifically necessary for WebSocket requests
    /// and batched requests, in which case this code executes allocation-free -- but it may be useful when GraphQL
    /// is served via a MVC controller action, for example.
    /// </summary>
    private class UserContextConfigurator : IConfigureExecution
    {
        public Task<ExecutionResult> ExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
        {
            if (options.UserContext == null || options.UserContext.Count == 0 && options.UserContext.GetType() == typeof(Dictionary<string, object>))
            {
                return SetAndExecuteAsync(options, next);
            }
            return next(options);
        }

        private static async Task<ExecutionResult> SetAndExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
        {
            var requestServices = options.RequestServices ?? throw new MissingRequestServicesException();
            var httpContext = requestServices.GetRequiredService<IHttpContextAccessor>().HttpContext!;
            var contextBuilder = requestServices.GetRequiredService<IUserContextBuilder>();
            var userContext = await contextBuilder.BuildUserContextAsync(httpContext, null);
            if (userContext != null)
                options.UserContext = userContext;
            return await next(options);
        }

        public float SortOrder => 100;
    }

    /// <summary>
    /// Registers <typeparamref name="TWebSocketAuthenticationService"/> with the dependency injection framework
    /// as a singleton of type <see cref="IWebSocketAuthenticationService"/>.
    /// </summary>
    public static IGraphQLBuilder AddWebSocketAuthentication<TWebSocketAuthenticationService>(this IGraphQLBuilder builder)
        where TWebSocketAuthenticationService : class, IWebSocketAuthenticationService
    {
        builder.Services.Register<IWebSocketAuthenticationService, TWebSocketAuthenticationService>(DI.ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers a service of type <see cref="IWebSocketAuthenticationService"/> with the specified factory delegate
    /// with the dependency injection framework as a singleton.
    /// </summary>
    public static IGraphQLBuilder AddWebSocketAuthentication(this IGraphQLBuilder builder, Func<IServiceProvider, IWebSocketAuthenticationService> factory)
    {
        builder.Services.Register(factory, DI.ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Registers a specified instance of type <see cref="IWebSocketAuthenticationService"/> with the
    /// dependency injection framework.
    /// </summary>
    public static IGraphQLBuilder AddWebSocketAuthentication(this IGraphQLBuilder builder, IWebSocketAuthenticationService webSocketAuthenticationService)
    {
        builder.Services.Register(webSocketAuthenticationService);
        return builder;
    }

    /// <summary>
    /// Registers <see cref="AuthorizationValidationRule"/> with the dependency injection framework
    /// and configures it to be used when executing a request.
    /// </summary>
    public static IGraphQLBuilder AddAuthorizationRule(this IGraphQLBuilder builder)
    {
        builder.AddValidationRule<AuthorizationValidationRule>(true);
        return builder;
    }
}

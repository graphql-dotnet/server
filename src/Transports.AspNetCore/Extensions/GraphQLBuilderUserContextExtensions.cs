using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server;

public static class GraphQLBuilderUserContextExtensions
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

        private async Task<ExecutionResult> SetAndExecuteAsync(ExecutionOptions options, ExecutionDelegate next)
        {
            var requestServices = options.RequestServices ?? throw new MissingRequestServicesException();
            var httpContext = requestServices.GetRequiredService<IHttpContextAccessor>().HttpContext!;
            var contextBuilder = requestServices.GetRequiredService<IUserContextBuilder>();
            options.UserContext = await contextBuilder.BuildUserContextAsync(httpContext, null);
            return await next(options);
        }
    }

    /// <summary>
    /// Set up default policy for matching endpoints. It is required when both GraphQL HTTP and
    /// GraphQL WebSockets middlewares are mapped to the same endpoint (by default 'graphql').
    /// </summary>
    /// <param name="builder">The GraphQL builder.</param>
    /// <returns>The GraphQL builder.</returns>
    public static IGraphQLBuilder AddDefaultEndpointSelectorPolicy(this IGraphQLBuilder builder)
    {
        builder.Services.TryRegister<MatcherPolicy, GraphQLDefaultEndpointSelectorPolicy>(DI.ServiceLifetime.Singleton, RegistrationCompareMode.ServiceTypeAndImplementationType);

        return builder;
    }
}

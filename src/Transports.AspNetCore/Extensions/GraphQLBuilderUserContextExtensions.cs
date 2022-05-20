using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server;

public static class GraphQLBuilderUserContextExtensions
{
    /// <summary>
    /// Adds an <see cref="IUserContextBuilder"/> as a singleton.
    /// </summary>
    /// <typeparam name="TUserContextBuilder">The type of the <see cref="IUserContextBuilder"/> implementation.</typeparam>
    /// <param name="builder">The GraphQL builder.</param>
    /// <returns>The GraphQL builder.</returns>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContextBuilder>(this IGraphQLBuilder builder)
        where TUserContextBuilder : class, IUserContextBuilder
    {
        builder.Services.Register<IUserContextBuilder, TUserContextBuilder>(DI.ServiceLifetime.Singleton);
        builder.ConfigureExecutionOptions(async options =>
        {
            if (options.UserContext == null || options.UserContext.Count == 0 && options.UserContext.GetType() == typeof(Dictionary<string, object>))
            {
                var requestServices = options.RequestServices ?? throw new MissingRequestServicesException();
                var httpContext = requestServices.GetRequiredService<IHttpContextAccessor>().HttpContext!;
                var contextBuilder = requestServices.GetRequiredService<IUserContextBuilder>();
                options.UserContext = await contextBuilder.BuildUserContextAsync(httpContext);
            }
        });

        return builder;
    }

    /// <summary>
    /// Set up a delegate to create the UserContext for each GraphQL request
    /// </summary>
    /// <typeparam name="TUserContext"></typeparam>
    /// <param name="builder">The GraphQL builder.</param>
    /// <param name="creator">A delegate used to create the user context from the <see cref="HttpContext"/>.</param>
    /// <returns>The GraphQL builder.</returns>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, TUserContext> creator)
        where TUserContext : class, IDictionary<string, object?>
    {
        builder.Services.Register<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator));
        builder.ConfigureExecutionOptions(options =>
        {
            if (options.UserContext == null || options.UserContext.Count == 0 && options.UserContext.GetType() == typeof(Dictionary<string, object>))
            {
                var requestServices = options.RequestServices ?? throw new MissingRequestServicesException();
                var httpContext = requestServices.GetRequiredService<IHttpContextAccessor>().HttpContext!;
                options.UserContext = creator(httpContext);
            }
        });

        return builder;
    }

    /// <summary>
    /// Set up a delegate to create the UserContext for each GraphQL request
    /// </summary>
    /// <typeparam name="TUserContext"></typeparam>
    /// <param name="builder">The GraphQL builder.</param>
    /// <param name="creator">A delegate used to create the user context from the <see cref="HttpContext"/>.</param>
    /// <returns>The GraphQL builder.</returns>
    public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, Task<TUserContext>> creator)
        where TUserContext : class, IDictionary<string, object?>
    {
        builder.Services.Register<IUserContextBuilder>(new UserContextBuilder<TUserContext>(context => new(creator(context))));
        builder.ConfigureExecutionOptions(async options =>
        {
            if (options.UserContext == null || options.UserContext.Count == 0 && options.UserContext.GetType() == typeof(Dictionary<string, object>))
            {
                var requestServices = options.RequestServices ?? throw new MissingRequestServicesException();
                var httpContext = requestServices.GetRequiredService<IHttpContextAccessor>().HttpContext!;
                options.UserContext = await creator(httpContext);
            }
        });

        return builder;
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

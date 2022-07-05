using GraphQL.DI;
using GraphQL.Server.Authorization.AspNetCore;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQL.Server;

public static class GraphQLBuilderAuthorizationExtensions
{
    /// <summary>
    /// Adds the GraphQL authorization.
    /// </summary>
    /// <param name="builder">The GraphQL builder.</param>
    /// <returns>Reference to the passed <paramref name="builder"/>.</returns>
    [Obsolete("This extension method has been replaced with AddAuthorization and will be removed in v8.")]
    public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder)
#if NETCOREAPP3_1_OR_GREATER
        => builder.AddGraphQLAuthorization(_ => { });

    /// <summary>
    /// Adds the GraphQL authorization.
    /// </summary>
    /// <param name="builder">The GraphQL builder.</param>
    /// <param name="configure">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
    /// <returns>Reference to the passed <paramref name="builder"/>.</returns>
    [Obsolete("This extension method has been replaced with AddAuthorization and will be removed in v8.")]
    public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder, Action<AuthorizationOptions>? configure)
#endif
    {
        if (builder.Services is not IServiceCollection services)
            throw new NotSupportedException("This method only supports the MicrosoftDI implementation of IGraphQLBuilder.");

        services.TryAddTransient<IClaimsPrincipalAccessor, DefaultClaimsPrincipalAccessor>();
        services.TryAddTransient<IAuthorizationErrorMessageBuilder, DefaultAuthorizationErrorMessageBuilder>();
        services.AddHttpContextAccessor();

#if NETCOREAPP3_1_OR_GREATER
        if (configure != null)
            services.AddAuthorizationCore(configure);
        else
            services.AddAuthorizationCore();
#endif

        builder.AddValidationRule<AuthorizationValidationRule>();

        return builder;
    }
}

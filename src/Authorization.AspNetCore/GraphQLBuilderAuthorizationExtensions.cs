#nullable enable

using GraphQL.DI;
using GraphQL.Server.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;
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
    public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder)
        => builder.AddGraphQLAuthorization(_ => { });

    /// <summary>
    /// Adds the GraphQL authorization.
    /// </summary>
    /// <param name="builder">The GraphQL builder.</param>
    /// <param name="configure">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
    /// <returns>Reference to the passed <paramref name="builder"/>.</returns>
    public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder, Action<AuthorizationOptions>? configure)
    {
        if (builder.Services is not IServiceCollection services)
            throw new NotSupportedException("This method only supports the MicrosoftDI implementation of IGraphQLBuilder.");

        services.TryAddTransient<IClaimsPrincipalAccessor, DefaultClaimsPrincipalAccessor>();
        services.TryAddTransient<IAuthorizationErrorMessageBuilder, DefaultAuthorizationErrorMessageBuilder>();
        services.AddHttpContextAccessor();

        if (configure != null)
            services.AddAuthorizationCore(configure);
        else
            services.AddAuthorizationCore();

        builder.AddValidationRule<AuthorizationValidationRule>(true);

        return builder;
    }
}

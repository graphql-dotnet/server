using System;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Adds the GraphQL authorization.
        /// </summary>
        /// <param name="builder">The GraphQL builder.</param>
        /// <returns>Reference to the passed <paramref name="builder"/>.</returns>
        public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder)
            => builder.AddGraphQLAuthorization(options => { });

        /// <summary>
        /// Adds the GraphQL authorization.
        /// </summary>
        /// <param name="builder">The GraphQL builder.</param>
        /// <param name="options">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
        /// <returns>Reference to the passed <paramref name="builder"/>.</returns>
        public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder, Action<AuthorizationOptions> options)
        {
            builder.Services
                .AddHttpContextAccessor()
                .AddTransient<IValidationRule, AuthorizationValidationRule>()
#if NETCOREAPP3_0
                .AddAuthorizationCore(options);
#else
                .AddAuthorization(options);
#endif

            return builder;
        }
    }
}

using System;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public static class GraphQlBuilderExtensions
    {
        /// <summary>
        /// Adds the GraphQL authorization.
        /// </summary>
        /// <param name="builder">The GraphQL builder.</param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder)
        {
            builder
                .Services
                .AddTransient<IValidationRule, AuthorizationValidationRule>()
                .AddAuthorization();
            return builder;
        }

        /// <summary>
        /// Adds the GraphQL authorization.
        /// </summary>
        /// <param name="builder">The GraphQL builder.</param>
        /// <param name="options">An action delegate to configure the provided <see cref="AuthorizationOptions"/>.</param>
        /// <returns>The GraphQL builder.</returns>
        public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder, Action<AuthorizationOptions> options)
        {
            builder
                .Services
                .AddTransient<IValidationRule, AuthorizationValidationRule>()
                .AddAuthorization(options);
            return builder;
        }
    }
}

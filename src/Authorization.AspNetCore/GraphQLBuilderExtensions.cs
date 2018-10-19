using System;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Adds the GraphQL authorization.
        /// </summary>
        /// <param name="builder">The GraphQL builder.</param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphQLAuthorization(this IGraphQLBuilder builder)
        {
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
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
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder
                .Services
                .AddTransient<IValidationRule, AuthorizationValidationRule>()
                .AddAuthorization(options);
            return builder;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
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
            builder.Services.AddSingleton<IUserContextBuilder, TUserContextBuilder>();

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
            where TUserContext : class, IDictionary<string, object>
        {
            builder.Services.AddSingleton<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator));

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
            where TUserContext : class, IDictionary<string, object>
        {
            builder.Services.AddSingleton<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator));

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
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, GraphQLDefaultEndpointSelectorPolicy>());

            return builder;
        }
    }
}

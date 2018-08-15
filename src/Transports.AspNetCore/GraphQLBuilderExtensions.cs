using System;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.Internal;
using GraphQL.Types.Relay;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Set up a delegate to create the UserContext for each GraphQL request
        /// </summary>
        /// <typeparam name="TUserContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, TUserContext> creator)
            where TUserContext : class
        {
            builder.Services.AddSingleton<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator));

            return builder;
        }

        /// <summary>
        /// Set up a delegate to create the UserContext for each GraphQL request
        /// </summary>
        /// <typeparam name="TUserContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="creator"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddUserContextBuilder<TUserContext>(this IGraphQLBuilder builder, Func<HttpContext, Task<TUserContext>> creator)
            where TUserContext : class
        {
            builder.Services.AddSingleton<IUserContextBuilder>(new UserContextBuilder<TUserContext>(creator));

            return builder;
        }

        /// <summary>
        /// Adds the GraphQL Relay types <see cref="ConnectionType<>"/>, <see cref="EdgeType<>"/>
        /// and <see cref="PageInfoType"/>.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IGraphQLBuilder AddRelayGraphTypes(this IGraphQLBuilder builder)
        {
            builder
                .Services
                .AddSingleton(typeof(ConnectionType<>))
                .AddSingleton(typeof(EdgeType<>))
                .AddSingleton<PageInfoType>();
            return builder;
        }
    }
}

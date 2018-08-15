﻿using System.Linq;
using System.Reflection;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Add required services for GraphQL data loader support
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddDataLoader(this IGraphQLBuilder builder)
        {
            builder.Services.TryAddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            builder.Services.AddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>();

            return builder;
        }

        /// <summary>
        /// Add all types that implement <seealso cref="IGraphType"/> in the calling assembly
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder)
        {
            return builder.AddGraphTypes(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Add all types that implement <seealso cref="IGraphType"/> in the specified assembly
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphTypes(this IGraphQLBuilder builder, Assembly assembly)
        {
            // Register all GraphQL types
            foreach (var type in assembly.GetTypes()
                .Where(x => !x.IsAbstract && typeof(IGraphType).IsAssignableFrom(x)))
            {
                builder.Services.TryAddTransient(type);
            }

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

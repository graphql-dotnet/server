﻿using System.Linq;
using System.Reflection;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Types;
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
        /// <param name="serviceLifetime">The service lifetime to register the GraphQL types with</param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphTypes(
            this IGraphQLBuilder builder,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            return builder.AddGraphTypes(Assembly.GetCallingAssembly(), serviceLifetime);
        }

        /// <summary>
        /// Add all types that implement <seealso cref="IGraphType"/> in the specified assembly
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="assembly">The assembly to register all GraphQL types from</param>
        /// <param name="serviceLifetime">The service lifetime to register the GraphQL types with</param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphTypes(
            this IGraphQLBuilder builder,
            Assembly assembly,
            ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            // Register all GraphQL types
            foreach (var type in assembly.GetTypes()
                .Where(x => !x.IsAbstract && typeof(IGraphType).IsAssignableFrom(x)))
            {
                builder.Services.TryAdd(new ServiceDescriptor(type, type, serviceLifetime));
            }

            return builder;
        }
    }
}

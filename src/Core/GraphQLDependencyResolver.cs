using System;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    /// <summary>
    /// Provides dependency resolution for GraphQL using an <seealso cref="IServiceProvider"/>
    /// </summary>
    public sealed class GraphQLDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _services;

        public GraphQLDependencyResolver(IServiceProvider services)
        {
            _services = services;
        }

        public T Resolve<T>() => _services.GetService<T>();

        public object Resolve(Type type) => _services.GetService(type);
    }
}

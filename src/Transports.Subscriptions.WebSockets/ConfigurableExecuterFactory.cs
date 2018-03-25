using System;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets
{
    /// <summary>
    ///     Factory wihch creates and <see cref="IGraphQLExecuter" /> for TSchema
    /// </summary>
    public class ConfigurableExecuterFactory<TSchema> : IGraphQLExecuterFactory<TSchema> where TSchema : ISchema
    {
        private readonly IServiceProvider _services;

        public ConfigurableExecuterFactory(
            IServiceProvider services)
        {
            _services = services;
        }

        public IGraphQLExecuter Create()
        {
            //todo: this feels wrong...
            return _services.GetRequiredService<ConfigurableExecuter<TSchema>>();
        }
    }
}
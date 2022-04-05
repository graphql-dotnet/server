using System;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    internal sealed class NoopServiceScopeFactory : IServiceScopeFactory, IServiceScope
    {
        public static IServiceScopeFactory Instance { get; } = new NoopServiceScopeFactory();
        private NoopServiceScopeFactory() { }
        IServiceScope IServiceScopeFactory.CreateScope() => this;
        IServiceProvider IServiceScope.ServiceProvider => null;
        void IDisposable.Dispose() { }
    }
}

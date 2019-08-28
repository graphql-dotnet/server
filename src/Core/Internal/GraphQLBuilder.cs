using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Internal
{
    internal sealed class GraphQLBuilder : IGraphQLBuilder
    {
        public IServiceCollection Services { get; }

        public GraphQLBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}

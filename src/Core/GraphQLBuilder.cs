using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
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

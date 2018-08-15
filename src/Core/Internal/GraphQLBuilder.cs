using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Internal
{
    public class GraphQLBuilder : IGraphQLBuilder
    {
        public IServiceCollection Services { get; }

        public GraphQLBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}

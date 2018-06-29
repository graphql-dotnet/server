using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Core
{
    internal class GraphQLBuilder : IGraphQLBuilder
    {
        public IServiceCollection Services { get; }
        
        public GraphQLBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}

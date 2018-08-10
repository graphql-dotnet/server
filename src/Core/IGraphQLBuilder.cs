using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public interface IGraphQLBuilder
    {
        IServiceCollection Services { get; }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Core
{
    public interface IGraphQLBuilder
    {
        IServiceCollection Services { get; }
    }
}

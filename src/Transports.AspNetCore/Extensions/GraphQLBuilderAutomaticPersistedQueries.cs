using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;

namespace GraphQL.Server;

/// <summary>
/// GraphQL specific extension methods for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class GraphQLBuilderAutomaticPersistedQueries
{
    public static IGraphQLBuilder AddAutomaticPersistedQueries(this IGraphQLBuilder builder, Action<AutomaticPersistedQueryCacheOptions> action = null)
    {
        builder.Services.Configure(action);
        builder.Services.TryRegister<IAutomaticPersistedQueryCache, AutomaticPersistedQueryCache>(ServiceLifetime.Singleton);
        return builder;
    }
}

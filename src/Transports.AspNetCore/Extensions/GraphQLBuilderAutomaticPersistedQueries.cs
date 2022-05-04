using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;

namespace GraphQL.Server;

/// <summary>
/// GraphQL specific extension methods for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class GraphQLBuilderAutomaticPersistedQueries
{
    public static IGraphQLBuilder AddAutomaticPersistedQueries(this IGraphQLBuilder builder, Action<AutomaticPersistedQueriesCacheOptions> action = null)
    {
        builder.Services.Configure(action);
        builder.Services.TryRegister<IAutomaticPersistedQueriesCache, AutomaticPersistedQueriesCache>(ServiceLifetime.Singleton);
        return builder;
    }
}

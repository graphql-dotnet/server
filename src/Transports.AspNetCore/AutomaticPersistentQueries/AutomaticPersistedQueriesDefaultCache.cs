namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueriesDefaultCache : IAutomaticPersistedQueriesCache
{
    public ValueTask<string> GetQueryByHash(string hash) => default;

    public ValueTask SetQueryByHash(string hash, string query) => default;
}

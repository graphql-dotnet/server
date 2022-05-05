namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueryDefaultCache : IAutomaticPersistedQueryCache
{
    public ValueTask<string> GetQuery(string hash) => default;

    public ValueTask<bool> SetQuery(string hash, string query) => new ValueTask<bool>(true);
}

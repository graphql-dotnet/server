namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueryDefaultCache : IAutomaticPersistedQueryCache
{
    public ValueTask<string> GetQuery(string hash) => default;

    public ValueTask<bool> SetQuery(string hash, string query)
    {
#if NET5_0_OR_GREATER
        return ValueTask.FromResult(true);
#else
        return new ValueTask<bool>(true);
#endif
    }
}

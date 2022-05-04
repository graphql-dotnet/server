using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueriesCache : IAutomaticPersistedQueriesCache
{
    private readonly MemoryCache _memoryCache;

    public AutomaticPersistedQueriesCache(IOptions<AutomaticPersistedQueriesCacheOptions> options)
    {
        _memoryCache = new MemoryCache(options);
    }

    public ValueTask<string> GetQueryByHash(string hash) => new ValueTask<string>(_memoryCache.Get<string>(hash));

    public ValueTask SetQueryByHash(string hash, string query)
    {
        _memoryCache.Set(hash, query);
        return default;
    }
}

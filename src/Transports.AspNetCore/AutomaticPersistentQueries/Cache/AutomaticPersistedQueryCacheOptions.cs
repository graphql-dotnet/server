using Microsoft.Extensions.Caching.Memory;

namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueryCacheOptions : MemoryCacheOptions
{
    public TimeSpan? SlidingExpiration { get; set; }
}

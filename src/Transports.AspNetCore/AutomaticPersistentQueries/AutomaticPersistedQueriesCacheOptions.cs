using Microsoft.Extensions.Caching.Memory;

namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueriesCacheOptions : MemoryCacheOptions
{
    public TimeSpan? SlidingExpiration { get; set; }
}

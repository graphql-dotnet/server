using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.AspNetCore;

public class AutomaticPersistedQueryCache : IAutomaticPersistedQueryCache
{
    private readonly MemoryCache _memoryCache;
    private readonly AutomaticPersistedQueryCacheOptions _options;

    public AutomaticPersistedQueryCache(IOptions<AutomaticPersistedQueryCacheOptions> options)
    {
        _options = options.Value;
        _memoryCache = new MemoryCache(options);
    }

    public ValueTask<string> GetQuery(string hash)
    {
        var result = _memoryCache.Get<string>(hash);

#if NET5_0_OR_GREATER
        return ValueTask.FromResult(result);
#else
        return new ValueTask<string>(result);
#endif
    }

    public ValueTask<bool> SetQuery(string hash, string query)
    {
        var result = false;

        if (hash.Equals(ComputeQuerySHA256(query), StringComparison.InvariantCultureIgnoreCase))
        {
            _memoryCache.Set(hash, query, new MemoryCacheEntryOptions { SlidingExpiration = _options.SlidingExpiration });
            result = true;
        }

#if NET5_0_OR_GREATER
        return ValueTask.FromResult(result);
#else
        return new ValueTask<bool>(result);
#endif
    }

    protected virtual string ComputeQuerySHA256(string query) => ComputeSHA256(query);

    public static string ComputeSHA256(string input)
    {
        var bytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

        var builder = new StringBuilder();
        foreach (var item in bytes)
        {
            builder.Append(item.ToString("x2"));
        }

        return builder.ToString();
    }
}

#if NETSTANDARD2_0

namespace GraphQL.Server.Transports.AspNetCore;

internal static class DictionaryExtensions
{
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
    {
        if (!dic.ContainsKey(key))
        {
            dic.Add(key, value);
            return true;
        }
        return false;
    }
}

#endif

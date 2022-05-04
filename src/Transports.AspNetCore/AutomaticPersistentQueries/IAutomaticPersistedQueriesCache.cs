namespace GraphQL.Server.Transports.AspNetCore;

public interface IAutomaticPersistedQueriesCache
{
    ValueTask<string> GetQueryByHash(string hash);

    ValueTask SetQueryByHash(string hash, string query);
}

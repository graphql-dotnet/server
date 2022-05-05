namespace GraphQL.Server.Transports.AspNetCore;

public interface IAutomaticPersistedQueryCache
{
    ValueTask<string> GetQuery(string hash);

    ValueTask<bool> SetQuery(string hash, string query);
}

namespace GraphQL.Server.Transports.AspNetCore;

public class PersistedQueryNotFoundError : ExecutionError
{
    public PersistedQueryNotFoundError(string hash) : base($"Persisted query with '{hash}' hash was not found.")
    {
        Code = "PERSISTED_QUERY_NOT_FOUND";
    }
}

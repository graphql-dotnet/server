using GraphQL.Validation;

namespace GraphQL.Server.Transports.AspNetCore;

[Serializable]
public class PersistedQueryNotFoundError : ValidationError
{
    public PersistedQueryNotFoundError(string hash)
        : base($"Persisted query with '{hash}' hash was not found.")
    {
    }
}

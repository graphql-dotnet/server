using GraphQL.Validation;

namespace GraphQL.Server.Transports.AspNetCore;

[Serializable]
public class PersistedQueryBadHashError : ValidationError
{
    public PersistedQueryBadHashError(string hash)
        : base($"The '{hash}' hash doesn't correspond to a query.")
    {
    }
}

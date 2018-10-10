using System.Security.Claims;

namespace GraphQL.Server.Authorization.AspNetCore
{
    public interface IProvideClaimsPrincipal
    {
        ClaimsPrincipal User { get; }
    }
}
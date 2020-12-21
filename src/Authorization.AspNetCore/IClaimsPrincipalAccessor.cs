using System.Security.Claims;
using GraphQL.Validation;

namespace GraphQL.Server.Authorization.AspNetCore
{
    public interface IClaimsPrincipalAccessor
    {
        ClaimsPrincipal GetClaimsPrincipal(ValidationContext context);
    }
}
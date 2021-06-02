using System.Security.Claims;
using GraphQL.Validation;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// Provides access to the <see cref="ClaimsPrincipal"/> used for GraphQL operation authorization.
    /// </summary>
    public interface IClaimsPrincipalAccessor
    {
        /// <summary>
        /// Provides the <see cref="ClaimsPrincipal"/> for the current <see cref="ValidationContext"/>
        /// </summary>
        /// <param name="context">The <see cref="ValidationContext"/> of the current operation</param>
        /// <returns></returns>
        ClaimsPrincipal GetClaimsPrincipal(ValidationContext context);
    }
}

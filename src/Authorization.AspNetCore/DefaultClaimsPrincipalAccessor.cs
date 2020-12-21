using System.Security.Claims;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Authorization.AspNetCore
{
    public class DefaultClaimsPrincipalAccessor: IClaimsPrincipalAccessor
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public DefaultClaimsPrincipalAccessor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public ClaimsPrincipal GetClaimsPrincipal(ValidationContext context)
        {
            return _contextAccessor.HttpContext.User;
        }
    }
}

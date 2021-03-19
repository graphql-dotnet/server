using System;
using System.Security.Claims;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// The default claims principal accessor.
    /// </summary>
    public class DefaultClaimsPrincipalAccessor : IClaimsPrincipalAccessor
    {
        private readonly IHttpContextAccessor _contextAccessor;

        /// <summary>
        /// Creates an instance of <see cref="DefaultClaimsPrincipalAccessor"/>.
        /// </summary>
        /// <param name="contextAccessor">ASP.NET Core <see cref="IHttpContextAccessor"/> to take claims principal (<see cref="HttpContext.User"/>) from.</param>
        public DefaultClaimsPrincipalAccessor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        }

        /// <summary>
        /// Returns the <see cref="HttpContext.User"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public ClaimsPrincipal GetClaimsPrincipal(ValidationContext context)
        {
            return _contextAccessor.HttpContext?.User;
        }
    }
}

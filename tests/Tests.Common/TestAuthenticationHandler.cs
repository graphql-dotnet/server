using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Tests.Common
{
    public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Look for test claim headers on the request
            List<Claim> claims = Request.Headers
                .Where(x => x.Key.StartsWith(TestAuthenticationDefaults.ClaimHeaderPrefix))
                .SelectMany(x => x.Value.Select(y => new Claim(x.Key.Substring(TestAuthenticationDefaults.ClaimHeaderPrefix.Length), y)))
                .ToList();

            if (claims.Count == 0)
                return Task.FromResult(AuthenticateResult.NoResult());

            // Create the authentication ticket with the claims found on the request
            var identity = new ClaimsIdentity(claims, Scheme.Name, Options.NameClaimType, Options.RoleClaimType);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

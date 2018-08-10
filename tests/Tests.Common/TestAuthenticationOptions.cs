using Microsoft.AspNetCore.Authentication;

namespace GraphQL.Server.Tests.Common
{
    public class TestAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string NameClaimType { get; set; }
        public string RoleClaimType { get; set; }
    }
}

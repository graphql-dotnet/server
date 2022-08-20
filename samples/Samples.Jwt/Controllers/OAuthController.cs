using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Samples.Jwt.Controllers;

public class OAuthController : Controller
{
    // sample OAuth2-compatible authorization endpoint supporting the 'client credentials' flow
    [HttpGet]
    [HttpPost]
    [Route("token")]
    public IActionResult Token([Bind(Prefix = "grant_type")] string grantType, [Bind(Prefix = "client_id")] string clientId, [Bind(Prefix = "client_secret")] string clientSecret)
    {
        // validate the provided client id and client secret
        if (grantType == "client_credentials" && clientId == "sampleClientId" && clientSecret == "sampleSecret")
        {
            // provide a signed JWT token with an 'Administrator' role claim
            var token = JwtHelper.Instance.CreateSignedToken(new Claim("role", "Administrator"));
            return Json(new
            {
                access_token = token.Token,
                expires_in = token.ExpiresIn.TotalSeconds,
                token_type = "Bearer",
            });
        }

        return Unauthorized();
    }
}

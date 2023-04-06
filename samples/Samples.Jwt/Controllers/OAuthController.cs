using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Samples.Jwt.Controllers;

public class OAuthController : Controller
{
    private readonly JwtHelper _jwtHelper;

    public OAuthController(JwtHelper jwtHelper)
    {
        _jwtHelper = jwtHelper;
    }

    // sample OAuth2-compatible authorization endpoint supporting the 'client credentials' flow
    [HttpGet]
    [HttpPost]
    [Route("token")]
    public IActionResult Token(string grant_type, string client_id, string client_secret)
    {
        // validate the provided client id and client secret
        if (grant_type == "client_credentials" && client_id == "sampleClientId" && client_secret == "sampleSecret")
        {
            // provide a signed JWT token with an 'Administrator' role claim
            var token = _jwtHelper.CreateSignedToken(new Claim("role", "Administrator"));
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

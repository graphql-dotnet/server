using System.Security.Claims;
using GraphQL.Server.Transports.AspNetCore.WebSockets;
using GraphQL.Transport;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GraphQL.Server.Samples.Jwt;

/// <summary>
/// Authenticates WebSocket connections via the 'payload' of the initialization packet.
/// This is necessary because WebSocket connections initiated from the browser cannot
/// authenticate via HTTP headers.
/// <br/><br/>
/// This class is not used when authenticating over GET/POST.
/// </summary>
public class JwtWebSocketAuthenticationService : IWebSocketAuthenticationService
{
    private readonly IGraphQLSerializer _graphQLSerializer;

    public JwtWebSocketAuthenticationService(IGraphQLSerializer graphQLSerializer)
    {
        _graphQLSerializer = graphQLSerializer;
    }

    public Task AuthenticateAsync(IWebSocketConnection connection, string subProtocol, OperationMessage operationMessage)
    {
        try
        {
            // for connections authenticated via HTTP headers, no need to reauthenticate
            if (connection.HttpContext.User.Identity?.IsAuthenticated ?? false)
                return Task.CompletedTask;

            // attempt to read the 'Authorization' key from the payload object and verify it contains "Bearer: XXXXXXXX"
            var authPayload = _graphQLSerializer.ReadNode<AuthPayload>(operationMessage.Payload);
            if (authPayload != null && authPayload.Authorization != null && authPayload.Authorization.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                // pull the token from the value
                var token = authPayload.Authorization.Substring(7);
                // parse the token in the same manner that the .NET AddJwtBearer() method does
                var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
                var result = handler.ValidateToken(token, JwtHelper.Instance.TokenValidationParameters);
                if (result.IsValid)
                {
                    // convert JWT tokens with "role" as the claim type (a JWT defined claim type) to the proper .NET claim type constant http://schemas.microsoft.com/ws/2008/06/identity/claims/role
                    // note this conversion automatically happens within the .NET AddJwtBearer() method when authenticating GET/POST requests
                    var claims = result.ClaimsIdentity.Claims.Select(claim => claim.Type == "role" ? new Claim(result.ClaimsIdentity.RoleClaimType, claim.Value) : claim);
                    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme));

                    // set the ClaimsPrincipal for the HttpContext; authentication will take place against this object
                    connection.HttpContext.User = principal;
                }
            }
        }
        catch
        {
            // no errors during authentication should throw an exception
            // specifically, attempting to validate an invalid JWT token will result in an exception, which may be logged or simply ignored to not generate an inordinate amount of logs without purpose
        }

        return Task.CompletedTask;
    }

    private class AuthPayload
    {
        public string? Authorization { get; set; }
    }
}

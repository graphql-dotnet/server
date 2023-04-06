using GraphQL.Server.Transports.AspNetCore.WebSockets;
using GraphQL.Transport;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GraphQL.Server.Samples.Jwt;

/// <summary>
/// Authenticates WebSocket connections via the 'payload' of the initialization packet.
/// This is necessary because WebSocket connections initiated from the browser cannot
/// authenticate via HTTP headers.
/// <br/><br/>
/// Notes:
/// <list type="bullet">
/// <item>This class is not used when authenticating over GET/POST.</item>
/// <item>
/// This class pulls the <see cref="TokenValidationParameters"/> instance from the instance of
/// <see cref="IOptionsMonitor{TOptions}">IOptionsMonitor</see>&lt;<see cref="JwtBearerOptions"/>&gt; registered
/// by ASP.NET Core during the call to <see cref="JwtBearerExtensions.AddJwtBearer(Microsoft.AspNetCore.Authentication.AuthenticationBuilder, Action{JwtBearerOptions})">AddJwtBearer</see>.
/// </item>
/// <item>
/// The expected format of the payload is <c>{"Authorization":"Bearer TOKEN"}</c> where TOKEN is the JSON Web Token (JWT),
/// mirroring the format of the 'Authorization' HTTP header.
/// </item>
/// </list>
/// </summary>
public class JwtWebSocketAuthenticationService : IWebSocketAuthenticationService
{
    private readonly IGraphQLSerializer _graphQLSerializer;
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtBearerOptionsMonitor;

    public JwtWebSocketAuthenticationService(IGraphQLSerializer graphQLSerializer, IOptionsMonitor<JwtBearerOptions> jwtBearerOptionsMonitor)
    {
        _graphQLSerializer = graphQLSerializer;
        _jwtBearerOptionsMonitor = jwtBearerOptionsMonitor;
    }

    public Task AuthenticateAsync(IWebSocketConnection connection, string subProtocol, OperationMessage operationMessage)
    {
        try
        {
            // for connections authenticated via HTTP headers, no need to reauthenticate
            if (connection.HttpContext.User.Identity?.IsAuthenticated ?? false)
                return Task.CompletedTask;

            // attempt to read the 'Authorization' key from the payload object and verify it contains "Bearer XXXXXXXX"
            var authPayload = _graphQLSerializer.ReadNode<AuthPayload>(operationMessage.Payload);
            if (authPayload != null && authPayload.Authorization != null && authPayload.Authorization.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                // pull the token from the value
                var token = authPayload.Authorization.Substring(7);
                // parse the token in the same manner that the .NET AddJwtBearer() method does:
                // JwtSecurityTokenHandler maps the 'name' and 'role' claims to the 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
                // and 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' claims;
                // this mapping is not performed by Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var tokenValidationParameters = _jwtBearerOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
                var principal = handler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                // set the ClaimsPrincipal for the HttpContext; authentication will take place against this object
                connection.HttpContext.User = principal;
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

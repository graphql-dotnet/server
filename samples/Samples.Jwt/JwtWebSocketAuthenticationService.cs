// Parts of this code file are based on the JwtBearerHandler class in the Microsoft.AspNetCore.Authentication.JwtBearer package found at:
//   https://github.com/dotnet/aspnetcore/blob/5493b413d1df3aaf00651bdf1cbd8135fa63f517/src/Security/Authentication/JwtBearer/src/JwtBearerHandler.cs
//
// Those sections of code may be subject to the MIT license found at:
//   https://github.com/dotnet/aspnetcore/blob/5493b413d1df3aaf00651bdf1cbd8135fa63f517/LICENSE.txt

using System.Security.Claims;
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
/// This class pulls the <see cref="JwtBearerOptions"/> instance registered by ASP.NET Core during the call to
/// <see cref="JwtBearerExtensions.AddJwtBearer(Microsoft.AspNetCore.Authentication.AuthenticationBuilder, Action{JwtBearerOptions})">AddJwtBearer</see>
/// for the <see cref="JwtBearerDefaults.AuthenticationScheme">Bearer</see> scheme and authenticates the token
/// based on simplified logic used by <see cref="JwtBearerHandler"/>.
/// </item>
/// <item>
/// The expected format of the payload is <c>{"Authorization":"Bearer TOKEN"}</c> where TOKEN is the JSON Web Token (JWT),
/// mirroring the format of the 'Authorization' HTTP header.
/// </item>
/// <item>
/// This implementation only supports the "Bearer" scheme configured in ASP.NET Core. Any scheme configured via
/// <see cref="Transports.AspNetCore.GraphQLHttpMiddlewareOptions.AuthenticationSchemes"/> property is
/// ignored by this implementation.
/// </item>
/// <item>
/// Events configured in <see cref="JwtBearerOptions.Events"/> are not raised by this implementation.
/// </item>
/// </list>
/// </summary>
public class JwtWebSocketAuthenticationService : IWebSocketAuthenticationService
{
    private readonly IGraphQLSerializer _graphQLSerializer;
    private readonly IOptionsMonitor<JwtBearerOptions> _jwtBearerOptionsMonitor;

    // This implementation currently only supports the "Bearer" scheme configured in ASP.NET Core
    private static string _scheme => JwtBearerDefaults.AuthenticationScheme;

    public JwtWebSocketAuthenticationService(IGraphQLSerializer graphQLSerializer, IOptionsMonitor<JwtBearerOptions> jwtBearerOptionsMonitor)
    {
        _graphQLSerializer = graphQLSerializer;
        _jwtBearerOptionsMonitor = jwtBearerOptionsMonitor;
    }

    public async Task AuthenticateAsync(IWebSocketConnection connection, string subProtocol, OperationMessage operationMessage)
    {
        try
        {
            // for connections authenticated via HTTP headers, no need to reauthenticate
            if (connection.HttpContext.User.Identity?.IsAuthenticated ?? false)
                return;

            // attempt to read the 'Authorization' key from the payload object and verify it contains "Bearer XXXXXXXX"
            var authPayload = _graphQLSerializer.ReadNode<AuthPayload>(operationMessage.Payload);
            if (authPayload != null && authPayload.Authorization != null && authPayload.Authorization.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                // pull the token from the value
                var token = authPayload.Authorization.Substring(7);

                var options = _jwtBearerOptionsMonitor.Get(_scheme);

                // follow logic simplified from JwtBearerHandler.HandleAuthenticateAsync, as follows:
                var tokenValidationParameters = await SetupTokenValidationParametersAsync(options, connection.HttpContext).ConfigureAwait(false);
                if (!options.UseSecurityTokenValidators)
                {
                    foreach (var tokenHandler in options.TokenHandlers)
                    {
                        try
                        {
                            var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);
                            if (tokenValidationResult.IsValid)
                            {
                                var principal = new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
                                // set the ClaimsPrincipal for the HttpContext; authentication will take place against this object
                                connection.HttpContext.User = principal;
                                return;
                            }
                        }
                        catch
                        {
                            // no errors during authentication should throw an exception
                            // specifically, attempting to validate an invalid JWT token will result in an exception, which may be logged or simply ignored to not generate an inordinate amount of logs without purpose
                        }
                    }
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    foreach (var validator in options.SecurityTokenValidators)
                    {
                        if (validator.CanReadToken(token))
                        {
                            try
                            {
                                var principal = validator.ValidateToken(token, tokenValidationParameters, out _);
                                // set the ClaimsPrincipal for the HttpContext; authentication will take place against this object
                                connection.HttpContext.User = principal;
                                return;
                            }
                            catch
                            {
                                // no errors during authentication should throw an exception
                                // specifically, attempting to validate an invalid JWT token will result in an exception, which may be logged or simply ignored to not generate an inordinate amount of logs without purpose
                            }
                        }
                    }
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
        }
        catch
        {
            // no errors during authentication should throw an exception
            // specifically, attempting to validate an invalid JWT token will result in an exception, which may be logged or simply ignored to not generate an inordinate amount of logs without purpose
        }
    }

    private static async ValueTask<TokenValidationParameters> SetupTokenValidationParametersAsync(JwtBearerOptions options, HttpContext httpContext)
    {
        // Clone to avoid cross request race conditions for updated configurations.
        var tokenValidationParameters = options.TokenValidationParameters.Clone();

        if (options.ConfigurationManager is BaseConfigurationManager baseConfigurationManager)
        {
            tokenValidationParameters.ConfigurationManager = baseConfigurationManager;
        }
        else
        {
            if (options.ConfigurationManager != null)
            {
                // GetConfigurationAsync has a time interval that must pass before new http request will be issued.
                var configuration = await options.ConfigurationManager.GetConfigurationAsync(httpContext.RequestAborted);
                var issuers = new[] { configuration.Issuer };
                tokenValidationParameters.ValidIssuers = (tokenValidationParameters.ValidIssuers == null ? issuers : tokenValidationParameters.ValidIssuers.Concat(issuers));
                tokenValidationParameters.IssuerSigningKeys = (tokenValidationParameters.IssuerSigningKeys == null ? configuration.SigningKeys : tokenValidationParameters.IssuerSigningKeys.Concat(configuration.SigningKeys));
            }
        }

        return tokenValidationParameters;
    }

    private class AuthPayload
    {
        public string? Authorization { get; set; }
    }
}

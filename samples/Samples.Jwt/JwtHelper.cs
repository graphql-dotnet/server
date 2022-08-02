using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace GraphQL.Server.Samples.Jwt;

/// <summary>
/// Provides a method to create a signed token, and provides token validation parameters to validate those tokens.
/// </summary>
public static class JwtHelper
{
    private static readonly SecurityKey _securityKey;
    private static readonly string _securityAlgorithm;
    private static readonly SigningCredentials _signingCredentials;
    private static readonly string _issuer = "http://localhost/Samples.Jwt";
    private static readonly string _audience = "Samples.Jwt.Audience";
    private static readonly TimeSpan _expiresIn = TimeSpan.FromMinutes(5);

    static JwtHelper()
    {
        // create a new random password (typically the password would be defined in an application secret)
        var password = Guid.NewGuid().ToString();
        // hash the password and use that to create a symmetric key for signing the JWT tokens
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        var keyBytes = SHA256.Create().ComputeHash(passwordBytes);
        _securityKey = new SymmetricSecurityKey(keyBytes);
        // define the algorithm and credentials
        _securityAlgorithm = SecurityAlgorithms.HmacSha256;
        _signingCredentials = new(_securityKey, _securityAlgorithm);
        // set the token validation parameters
        TokenValidationParameters =
            new TokenValidationParameters
            {
                // validate the issuer name on the token
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                // validate the audience name on the token
                ValidateAudience = true,
                ValidAudience = _audience,
                // validate the 'not before' timestamp, if it exists
                ValidateLifetime = true,
                // ensure the token has not expired
                RequireExpirationTime = true,
                // allow up to 6 seconds of clock skew (aka add 6 seconds to the expiration time)
                //   (because Azure might let the VM clock get 5 seconds off before forcing a re-sync to the host)
                ClockSkew = TimeSpan.FromSeconds(6),
                // ensure the digital signature exists and validate it
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = new[] { _securityKey },
                ValidAlgorithms = new[] { _securityAlgorithm },
            };
    }

    /// <summary>
    /// Returns the <see cref="TokenValidationParameters"/> used to authenticate JWT bearer tokens.
    /// </summary>
    public static TokenValidationParameters TokenValidationParameters { get; }

    /// <summary>
    /// Creates a signed JWT token containing the specified <see cref="Claim"/>s.
    /// </summary>
    public static (TimeSpan ExpiresIn, string Token) CreateSignedToken(params Claim[] claims)
    {
        var now = DateTime.UtcNow;

        // create the security token as follows:
        var token = new JwtSecurityToken(
            // issuer is an arbitary string, typically the name of the web server that issued the token
            issuer: _issuer,
            // audience is an arbitary string, typically representing valid recipients - typically 'Refresh' or 'Access' or a url for a subdomain
            audience: _audience,
            // include a list of claims
            claims: claims,
            // set the time this token becomes valid
            notBefore: now,
            // for access tokens, set a short timeout like 5 minutes, after which the access token will need to be refreshed
            //   (the access token can be refreshed before or after the expiration, as refreshing it uses the refresh token)
            // for refresh tokens, set a long timeout like 6 months, after which the refresh token will expire
            expires: now.Add(_expiresIn),
            // set the digital signature algorithm and key
            signingCredentials: _signingCredentials
        );

        // return the token
        return (_expiresIn, new JwtSecurityTokenHandler().WriteToken(token));
    }
}

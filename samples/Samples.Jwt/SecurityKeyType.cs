namespace GraphQL.Server.Samples.Jwt;

/// <summary>
/// Indicates the type of security
/// </summary>
public enum SecurityKeyType
{
    /// <summary>
    /// Represents a SHA256 symmetric security key, capable of creating and validating JWT tokens.
    /// </summary>
    SymmetricSecurityKey,

    /// <summary>
    /// Represents the public key of a ECDsa asymmetric security key, only capable of validating JWT tokens.
    /// </summary>
    PublicKey,

    /// <summary>
    /// Represents the private key of a ECDsa asymmetric security key, capable of creating and validating JWT tokens.
    /// </summary>
    PrivateKey,
}

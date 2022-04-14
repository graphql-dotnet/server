#nullable enable

namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error indicating that the content-type was invalid.
/// </summary>
public class InvalidContentTypeError : RequestError
{
    /// <inheritdoc cref="InvalidContentTypeError"/>
    public InvalidContentTypeError() : base("Invalid 'Content-Type' header.") { }

    /// <inheritdoc cref="InvalidContentTypeError"/>
    public InvalidContentTypeError(string message) : base("Invalid 'Content-Type' header: " + message) { }
}

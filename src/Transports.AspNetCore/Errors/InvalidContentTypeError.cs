namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error indicating that the content-type is invalid, for example, could not be parsed or is not supported.
/// </summary>
public class InvalidContentTypeError : RequestError
{
    /// <inheritdoc cref="InvalidContentTypeError"/>
    public InvalidContentTypeError() : base("Invalid 'Content-Type' header.") { }

    /// <inheritdoc cref="InvalidContentTypeError"/>
    public InvalidContentTypeError(string message) : base("Invalid 'Content-Type' header: " + message) { }
}

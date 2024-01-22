namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error when an invalid map path is provided in a GraphQL file upload request.
/// </summary>
public class InvalidMapError : RequestError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidMapError"/> class.
    /// </summary>
    public InvalidMapError(string message, Exception? innerException = null)
        : base("Invalid map path. " + message, innerException)
    {
    }
}

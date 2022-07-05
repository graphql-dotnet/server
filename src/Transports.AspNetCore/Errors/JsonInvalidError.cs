namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error indicating that the JSON provided could not be parsed.
/// </summary>
public class JsonInvalidError : RequestError
{
    /// <inheritdoc cref="JsonInvalidError"/>
    public JsonInvalidError() : base($"JSON body text could not be parsed.") { }

    /// <inheritdoc cref="JsonInvalidError"/>
    public JsonInvalidError(Exception innerException) : base($"JSON body text could not be parsed. {innerException.Message}") { }
}

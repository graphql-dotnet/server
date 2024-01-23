namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Defines an interface for errors that have a preferred HTTP status code.
/// </summary>
public interface IHasPreferredStatusCode
{
    /// <summary>
    /// Returns the preferred HTTP status code for this error.
    /// </summary>
    HttpStatusCode PreferredStatusCode { get; }
}

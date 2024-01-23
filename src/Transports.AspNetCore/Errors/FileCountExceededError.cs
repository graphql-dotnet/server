namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error when too many files are uploaded in a GraphQL request.
/// </summary>
public class FileCountExceededError : RequestError, IHasPreferredStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileCountExceededError"/> class.
    /// </summary>
    public FileCountExceededError()
        : base("File uploads exceeded.")
    {
    }

    /// <inheritdoc/>
    public HttpStatusCode PreferredStatusCode => HttpStatusCode.RequestEntityTooLarge;
}

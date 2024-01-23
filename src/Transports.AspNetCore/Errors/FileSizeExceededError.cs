namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error when a file exceeds the allowed size limit in a GraphQL upload.
/// </summary>
public class FileSizeExceededError : RequestError, IHasPreferredStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSizeExceededError"/> class.
    /// </summary>
    public FileSizeExceededError()
        : base("File size limit exceeded.")
    {
    }

    /// <inheritdoc/>
    public HttpStatusCode PreferredStatusCode => HttpStatusCode.RequestEntityTooLarge;
}

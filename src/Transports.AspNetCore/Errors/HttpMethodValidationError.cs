namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents a validation error indicating that the requested operation is not valid
/// for the type of HTTP request.
/// </summary>
public class HttpMethodValidationError : ValidationError
{
    /// <inheritdoc cref="HttpMethodValidationError"/>
    public HttpMethodValidationError(GraphQLParser.ROM originalQuery, ASTNode node, string message)
        : base(originalQuery, null!, message, node)
    {
    }
}

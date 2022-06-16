namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Validates that HTTP GET requests only execute queries; not mutations or subscriptions.
/// </summary>
public sealed class HttpGetValidationRule : IValidationRule
{
    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        if (context.Operation.Operation != OperationType.Query)
        {
            context.ReportError(new HttpMethodValidationError(context.Document.Source, context.Operation, "Only query operations allowed for GET requests."));
        }
        return default;
    }
}

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Validates that HTTP POST requests do not execute subscriptions.
/// </summary>
public sealed class HttpPostValidationRule : ValidationRuleBase
{
    /// <inheritdoc/>
    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context)
    {
        if (context.Operation.Operation == OperationType.Subscription)
        {
            context.ReportError(new HttpMethodValidationError(context.Document.Source, context.Operation, "Subscription operations are not supported for POST requests."));
        }
        return default;
    }
}

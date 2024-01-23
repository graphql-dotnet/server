namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Represents a GraphQL scalar type named 'FormFile' for handling file uploads
/// sent via multipart/form-data GraphQL requests.
/// </summary>
public class FormFileGraphType : ScalarGraphType
{
    /// <inheritdoc/>
    public override bool CanParseLiteral(GraphQLValue value) => value is GraphQLNullValue;

    /// <inheritdoc/>
    public override object? ParseLiteral(GraphQLValue value)
        => value is GraphQLNullValue ? null : ThrowLiteralConversionError(value, "Uploaded files must be passed through variables.");

    /// <inheritdoc/>
    public override bool CanParseValue(object? value) => value is IFormFile || value == null;

    /// <inheritdoc/>
    public override object? ParseValue(object? value) => value switch
    {
        IFormFile _ => value,
        null => null,
        _ => ThrowValueConversionError(value)
    };

    /// <inheritdoc/>
    public override object? Serialize(object? value) => value is null ? null :
        throw new InvalidOperationException("The FormFile scalar graph type cannot be used to return information from a GraphQL endpoint.");

    /// <inheritdoc/>
    public override bool IsValidDefault(object value) => false;

    /// <inheritdoc/>
    public override GraphQLValue ToAST(object? value) => value is null ? new GraphQLNullValue() :
        throw new InvalidOperationException("FormFile values cannot be converted to an AST node.");
}

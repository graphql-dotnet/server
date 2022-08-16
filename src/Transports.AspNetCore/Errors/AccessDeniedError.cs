namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error indicating that the user is not allowed access to the specified resource.
/// </summary>
public class AccessDeniedError : ValidationError
{
    /// <inheritdoc cref="AccessDeniedError"/>
    public AccessDeniedError(string resource)
        : base($"Access denied for {resource}.")
    {
    }

    /// <inheritdoc cref="AccessDeniedError"/>
    public AccessDeniedError(string resource, GraphQLParser.ROM originalQuery, params ASTNode[] nodes)
        : base(originalQuery, null!, $"Access denied for {resource}.", nodes)
    {
    }

    /// <summary>
    /// Returns the policy that would allow access to these node(s).
    /// </summary>
    public string? PolicyRequired { get; set; }

    /// <inheritdoc cref="AuthorizationResult"/>
    public AuthorizationResult? PolicyAuthorizationResult { get; set; }

    /// <summary>
    /// Returns the list of role memberships that would allow access to these node(s).
    /// </summary>
    public List<string>? RolesRequired { get; set; }
}

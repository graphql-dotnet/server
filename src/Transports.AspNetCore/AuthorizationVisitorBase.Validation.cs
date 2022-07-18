namespace GraphQL.Server.Transports.AspNetCore;

public partial class AuthorizationVisitorBase
{
    /// <summary>
    /// Validates authorization rules for the schema.
    /// Returns a value indicating if validation was successful.
    /// </summary>
    public virtual ValueTask<bool> ValidateSchemaAsync(ValidationContext context)
        => ValidateAsync(context.Schema, null, context);

    /// <summary>
    /// Validate a node that is current within the context.
    /// </summary>
    private ValueTask<bool> ValidateAsync(IProvideMetadata obj, ASTNode? node, ValidationContext context)
        => ValidateAsync(BuildValidationInfo(node, obj, context));

    /// <summary>
    /// Initializes a new <see cref="ValidationInfo"/> instance for the specified node.
    /// </summary>
    /// <param name="node">The specified <see cref="ASTNode"/>.</param>
    /// <param name="obj">The <see cref="IGraphType"/>, <see cref="IFieldType"/> or <see cref="QueryArgument"/> which has been matched to the node specified in <paramref name="node"/>.</param>
    /// <param name="context">The validation context.</param>
    private static ValidationInfo BuildValidationInfo(ASTNode? node, IProvideMetadata obj, ValidationContext context)
    {
        IFieldType? parentFieldType = null;
        IGraphType? parentGraphType = null;
        if (node is GraphQLField)
        {
            if (obj is IGraphType)
            {
                parentFieldType = context.TypeInfo.GetFieldDef(0);
                parentGraphType = context.TypeInfo.GetLastType(1)?.GetNamedType();
            }
            else if (obj is IFieldType)
            {
                parentGraphType = context.TypeInfo.GetLastType(1)?.GetNamedType();
            }
        }
        else if (node is GraphQLArgument)
        {
            parentFieldType = context.TypeInfo.GetFieldDef();
            parentGraphType = context.TypeInfo.GetLastType(1)?.GetNamedType();
        }
        return new(obj, node, parentFieldType, parentGraphType, context);
    }

    /// <summary>Provides contextual information to the schema, graph, field, or query argument being validated.</summary>
    /// <param name="Obj">The schema, graph type, field type, or query argument being validated. May be an interface type if fragments are in use.</param>
    /// <param name="Node">Null for a schema validation; otherwise the <see cref="GraphQLOperationDefinition"/>, <see cref="GraphQLField"/>, or <see cref="GraphQLArgument"/> being validated.</param>
    /// <param name="Context">The validation context; but <see cref="ValidationContext.TypeInfo"/> may not be applicable for node being validated.</param>
    /// <param name="ParentFieldType">For graph types other than operations, the field where this type was referenced; for query arguments, the field to which this argument belongs.</param>
    /// <param name="ParentGraphType">For graph types, the graph type for the field where this type was referenced; for field types, the graph type to which this field belongs; for query arguments, the graph type for the field to which this argument belongs.</param>
    public readonly record struct ValidationInfo(
        IProvideMetadata Obj,
        ASTNode? Node,
        IFieldType? ParentFieldType,
        IGraphType? ParentGraphType,
        ValidationContext Context);

    // contains cached authorization results
    private Dictionary<string, bool>? _roleResults; // contains a dictionary of roles that have been checked
    private Dictionary<string, AuthorizationResult>? _policyResults; // contains a dictionary of policies that have been checked
    private bool? _userIsAuthorized;

    /// <summary>
    /// Validates authorization rules for the specified schema, graph, field or query argument.
    /// Does not consider <see cref="AuthorizationExtensions.IsAnonymousAllowed(IProvideMetadata)"/>
    /// as this is handled elsewhere.
    /// Returns a value indicating if validation was successful for this node.
    /// </summary>
    protected virtual async ValueTask<bool> ValidateAsync(ValidationInfo info)
    {
        bool requiresAuthorization = info.Obj.IsAuthorizationRequired();
        if (!requiresAuthorization)
            return true;

        var authorized = _userIsAuthorized ??= IsAuthenticated;
        if (!authorized)
        {
            HandleNodeNotAuthorized(info);
            return false;
        }

        var policies = info.Obj.GetPolicies();
        if (policies?.Count > 0)
        {
            _policyResults ??= new Dictionary<string, AuthorizationResult>();
            foreach (var policy in policies)
            {
                if (!_policyResults.TryGetValue(policy, out var result))
                {
                    result = await AuthorizeAsync(policy);
                    _policyResults.Add(policy, result);
                }
                if (!result.Succeeded)
                {
                    HandleNodeNotInPolicy(info, policy, result);
                    return false;
                }
            }
        }

        var roles = info.Obj.GetRoles();
        if (roles?.Count > 0)
        {
            _roleResults ??= new Dictionary<string, bool>();
            foreach (var role in roles)
            {
                if (!_roleResults.TryGetValue(role, out var result))
                {
                    result = IsInRole(role);
                    _roleResults.Add(role, result);
                }
                if (result)
                    goto PassRoles;
            }
            HandleNodeNotInRoles(info, roles);
            return false;
        }
    PassRoles:

        return true;
    }

    /// <inheritdoc cref="IIdentity.IsAuthenticated"/>
    protected abstract bool IsAuthenticated { get; }

    /// <inheritdoc cref="ClaimsPrincipal.IsInRole(string)"/>
    protected abstract bool IsInRole(string role);

    /// <inheritdoc cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, string)"/>
    protected abstract ValueTask<AuthorizationResult> AuthorizeAsync(string policy);

    /// <summary>
    /// Adds a error to the validation context indicating that the user is not authenticated
    /// as required by this graph, field or query argument.
    /// </summary>
    /// <param name="info">Information about the node being validated.</param>
    protected virtual void HandleNodeNotAuthorized(ValidationInfo info)
    {
        var resource = GenerateResourceDescription(info);
        var err = info.Node == null ? new AccessDeniedError(resource) : new AccessDeniedError(resource, info.Context.Document.Source, info.Node);
        info.Context.ReportError(err);
    }

    /// <summary>
    /// Adds a error to the validation context indicating that the user is not a member of any of
    /// the roles required by this graph, field or query argument.
    /// </summary>
    /// <param name="info">Information about the node being validated.</param>
    /// <param name="roles">The list of roles of which the user must be a member.</param>
    protected virtual void HandleNodeNotInRoles(ValidationInfo info, List<string> roles)
    {
        var resource = GenerateResourceDescription(info);
        var err = info.Node == null ? new AccessDeniedError(resource) : new AccessDeniedError(resource, info.Context.Document.Source, info.Node);
        err.RolesRequired = roles;
        info.Context.ReportError(err);
    }

    /// <summary>
    /// Adds a error to the validation context indicating that the user does not meet the
    /// authorization policy required by this graph, field or query argument.
    /// </summary>
    /// <param name="info">Information about the node being validated.</param>
    /// <param name="policy">The policy which these nodes are being authenticated against.</param>
    /// <param name="authorizationResult">The result of the authentication request.</param>
    protected virtual void HandleNodeNotInPolicy(ValidationInfo info, string policy, AuthorizationResult authorizationResult)
    {
        var resource = GenerateResourceDescription(info);
        var err = info.Node == null ? new AccessDeniedError(resource) : new AccessDeniedError(resource, info.Context.Document.Source, info.Node);
        err.PolicyRequired = policy;
        err.PolicyAuthorizationResult = authorizationResult;
        info.Context.ReportError(err);
    }

    /// <summary>
    /// Generates a friendly name for a specified graph, field or query argument.
    /// </summary>
    protected virtual string GenerateResourceDescription(ValidationInfo info)
    {
        if (info.Obj is ISchema)
        {
            return "schema";
        }
        else if (info.Obj is IGraphType graphType)
        {
            if (info.Node is GraphQLField)
            {
                return $"type '{graphType.Name}' for field '{info.ParentFieldType?.Name}' on type '{info.ParentGraphType?.Name}'";
            }
            else if (info.Node is GraphQLOperationDefinition op)
            {
                return $"type '{graphType.Name}' for {op.Operation.ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture)} operation{(!string.IsNullOrEmpty(op.Name?.StringValue) ? $" '{op.Name}'" : null)}";
            }
            else
            {
                return $"type '{graphType.Name}'";
            }
        }
        else if (info.Obj is IFieldType fieldType)
        {
            return $"field '{fieldType.Name}' on type '{info.ParentGraphType?.Name}'";
        }
        else if (info.Obj is QueryArgument queryArgument)
        {
            return $"argument '{queryArgument.Name}' for field '{info.ParentFieldType?.Name}' on type '{info.ParentGraphType?.Name}'";
        }
        else
        {
            return info.Node?.GetType().Name ?? "unknown";
        }
    }
}

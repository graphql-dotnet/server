using System.Security.Claims;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Validates a document against the configured set of policy and role requirements.
/// </summary>
public class AuthorizationValidationRule : IValidationRule
{
    private readonly IHttpContextAccessor _contextAccessor;

    /// <inheritdoc cref="AuthorizationValidationRule"/>
    public AuthorizationValidationRule(IHttpContextAccessor httpContextAccessor)
    {
        _contextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <inheritdoc/>
    public ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        var httpContext = _contextAccessor.HttpContext ?? NoHttpContext();
        var user = httpContext.User ?? NoUser();
        var provider = context.RequestServices ?? NoRequestServices();
        var authService = provider.GetService<IAuthorizationService>() ?? NoAuthServiceError();

        var visitor = new AuthorizationVisitor(context, user, authService);
        return visitor.ValidateSchema(context) ? new(visitor) : default;
    }

    private static HttpContext NoHttpContext()
        => throw new InvalidOperationException("HttpContext could not be retrieved from IHttpContextAccessor.");

    private static ClaimsPrincipal NoUser()
        => throw new InvalidOperationException("ClaimsPrincipal could not be retrieved from HttpContext.User.");

    private static IServiceProvider NoRequestServices()
        => throw new MissingRequestServicesException();

    private static IAuthorizationService NoAuthServiceError()
        => throw new InvalidOperationException("An instance of IAuthorizationService could not be pulled from the dependency injection framework.");

    /// <inheritdoc cref="AuthorizationValidationRule"/>
    public class AuthorizationVisitor : INodeVisitor
    {
        /// <inheritdoc cref="AuthorizationVisitor"/>
        public AuthorizationVisitor(ValidationContext context, ClaimsPrincipal claimsPrincipal, IAuthorizationService authorizationService)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            ClaimsPrincipal = claimsPrincipal ?? throw new ArgumentNullException(nameof(claimsPrincipal));
            if (claimsPrincipal.Identity == null)
                throw new InvalidOperationException($"{nameof(claimsPrincipal)}.Identity cannot be null.");
            AuthorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _fragmentDefinitionsToCheck = context.GetRecursivelyReferencedFragments(context.Operation);
            _userIsAuthenticated = claimsPrincipal.Identity.IsAuthenticated;
        }

        /// <summary>
        /// The user that this authorization visitor will authenticate against.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; }

        /// <summary>
        /// The authorization service that is used to authorize policy requests.
        /// </summary>
        public IAuthorizationService AuthorizationService { get; }

        private bool _checkTree; // used to skip processing fragments that do not apply
        private ASTNode? _checkUntil;
        private readonly List<GraphQLFragmentDefinition>? _fragmentDefinitionsToCheck; // contains a list of fragments to process, or null if none
        private Dictionary<string, AuthorizationResult>? _policyResults; // contains a dictionary of policies that have been checked
        private Dictionary<string, bool>? _roleResults; // contains a dictionary of roles that have been checked
        private readonly Stack<TypeInfo> _onlyAnonymousSelected = new();
        private readonly bool _userIsAuthenticated;
        private Dictionary<string, TypeInfo>? _fragments;
        private List<TodoInfo>? _todo;

        private class TypeInfo
        {
            public bool AnyAuthenticated;
            public bool AnyAnonymous;
            public List<string>? WaitingOnFragments;
        }

        private class TodoInfo
        {
            public ValidationInfo ValidationInfo { get; }
            public bool AnyAuthenticated;
            public bool AnyAnonymous;
            public List<string> WaitingOnFragments;

            public TodoInfo(ValidationInfo vi, TypeInfo ti)
            {
                ValidationInfo = vi;
                AnyAuthenticated = ti.AnyAuthenticated;
                AnyAnonymous = ti.AnyAnonymous;
                WaitingOnFragments = ti.WaitingOnFragments ?? NoWaitingOnFragments();
            }

            private static List<string> NoWaitingOnFragments()
                => throw new InvalidOperationException("Waiting on fragments must not be null.");
        }

        /// <inheritdoc/>
        public virtual void Enter(ASTNode node, ValidationContext context)
        {
            if (node == context.Operation || (node is GraphQLFragmentDefinition fragmentDefinition && _fragmentDefinitionsToCheck != null && _fragmentDefinitionsToCheck.Contains(fragmentDefinition)))
            {
                var type = context.TypeInfo.GetLastType()?.GetNamedType();
                if (type != null)
                {
                    // if type is null that means that no type was configured for this operation in the schema; will produce a separate validation error
                    _onlyAnonymousSelected.Push(new());
                    _checkTree = true;
                }
            }
            else if (_checkTree)
            {
                if (node is GraphQLField fieldNode)
                {
                    if (SkipField(fieldNode, context))
                    {
                        _checkTree = false;
                        _checkUntil = node;
                    }
                    else
                    {
                        var field = context.TypeInfo.GetFieldDef();
                        // might be null if no match was found in the schema
                        // and skip processing for __typeName
                        if (field != null && field != context.Schema.TypeNameMetaFieldType)
                        {
                            var fieldAnonymousAllowed = field.IsAnonymousAllowed() || field == context.Schema.TypeMetaFieldType || field == context.Schema.SchemaMetaFieldType;
                            var ti = _onlyAnonymousSelected.Peek();
                            if (fieldAnonymousAllowed)
                                ti.AnyAnonymous = true;
                            else
                                ti.AnyAuthenticated = true;

                            if (!fieldAnonymousAllowed)
                            {
                                Validate(field, node, context);
                            }
                        }
                        // prep for descendants, if any
                        _onlyAnonymousSelected.Push(new());
                    }
                }
                else if (node is GraphQLFragmentSpread fragmentSpread)
                {
                    var ti = _onlyAnonymousSelected.Peek();
                    var fragmentName = fragmentSpread.FragmentName.Name.StringValue;
                    if (_fragments?.TryGetValue(fragmentName, out var fragmentInfo) == true)
                    {
                        ti.AnyAuthenticated |= fragmentInfo.AnyAuthenticated;
                        ti.AnyAnonymous |= fragmentInfo.AnyAnonymous;
                        if (fragmentInfo.WaitingOnFragments?.Count > 0)
                        {
                            ti.WaitingOnFragments ??= new();
                            ti.WaitingOnFragments.AddRange(fragmentInfo.WaitingOnFragments);
                        }
                    }
                    else
                    {
                        ti.WaitingOnFragments ??= new();
                        ti.WaitingOnFragments.Add(fragmentName);
                    }
                }
                else if (node is GraphQLArgument)
                {
                    // ignore arguments of directives
                    if (context.TypeInfo.GetAncestor(2)?.Kind == ASTNodeKind.Field)
                    {
                        // verify field argument
                        var arg = context.TypeInfo.GetArgument();
                        if (arg != null)
                        {
                            Validate(arg, node, context);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public virtual void Leave(ASTNode node, ValidationContext context)
        {
            if (!_checkTree)
            {
                if (_checkUntil == node)
                {
                    _checkTree = true;
                    _checkUntil = null;
                }
                return;
            }
            if (node == context.Operation)
            {
                _checkTree = false;
                PopAndProcess();
            }
            else if (node is GraphQLFragmentDefinition fragmentDefinition)
            {
                _checkTree = false;
                var fragmentName = fragmentDefinition.FragmentName.Name.StringValue;
                var ti = _onlyAnonymousSelected.Pop();
                RecursiveResolve(fragmentName, ti, context);
                _fragments ??= new();
                _fragments.TryAdd(fragmentName, ti);
            }
            else if (_checkTree && node is GraphQLField)
            {
                PopAndProcess();
            }

            void PopAndProcess()
            {
                var info = _onlyAnonymousSelected.Pop();
                var type = context.TypeInfo.GetLastType()?.GetNamedType();
                if (type == null)
                    return;
                if (info.AnyAuthenticated || (!info.AnyAnonymous && (info.WaitingOnFragments?.Count ?? 0) == 0))
                {
                    Validate(type, node, context);
                }
                else if (info.WaitingOnFragments?.Count > 0)
                {
                    _todo ??= new();
                    _todo.Add(new(BuildValidationInfo(type, node, context), info));
                }
            }
        }

        /// <summary>
        /// Indicates if the specified field should skip authentication processing.
        /// Default implementation looks at @skip and @include directives only.
        /// </summary>
        protected virtual bool SkipField(GraphQLField node, ValidationContext context)
        {
            // check 
            var skipDirective = node.Directives?.FirstOrDefault(x => x.Name == "skip");
            if (skipDirective != null)
            {
                var value = GetDirectiveValue(skipDirective, context, false);
                if (value)
                    return true;
            }

            var includeDirective = node.Directives?.FirstOrDefault(x => x.Name == "include");
            if (includeDirective != null)
            {
                var value = GetDirectiveValue(includeDirective, context, true);
                if (!value)
                    return true;
            }

            return false;

            static bool GetDirectiveValue(GraphQLDirective directive, ValidationContext context, bool defaultValue)
            {
                var ifArg = directive.Arguments?.FirstOrDefault(x => x.Name == "if");
                if (ifArg != null)
                {
                    if (ifArg.Value is GraphQLBooleanValue boolValue)
                    {
                        return boolValue.BoolValue;
                    }
                    else if (ifArg.Value is GraphQLVariable variable)
                    {
                        if (context.Operation.Variables != null)
                        {
                            var varDef = context.Operation.Variables.FirstOrDefault(x => x.Variable.Name == variable.Name);
                            if (varDef != null && varDef.Type.Name() == "Boolean")
                            {
                                if (context.Variables.TryGetValue(variable.Name.StringValue, out var value))
                                {
                                    if (value is bool boolValue2)
                                        return boolValue2;
                                }
                                if (varDef.DefaultValue is GraphQLBooleanValue boolValue3)
                                {
                                    return boolValue3.BoolValue;
                                }
                            }
                        }
                    }
                }
                return defaultValue;
            }
        }

        // runs when a fragment is added or updated; the fragment might not be waiting on any
        // other fragments, or it still might be
        private void RecursiveResolve(string fragmentName, TypeInfo ti, ValidationContext context)
        {
            // first see if any other fragments are waiting on this fragment
            if (_fragments != null)
            {
                foreach (var fragment in _fragments)
                {
                    var ti2 = fragment.Value;
                    if (ti2.WaitingOnFragments != null && ti2.WaitingOnFragments.Remove(fragmentName))
                    {
                        ti2.AnyAuthenticated |= ti.AnyAuthenticated;
                        ti2.AnyAnonymous |= ti.AnyAnonymous;
                        RecursiveResolve(fragment.Key, ti2, context);
                    }
                }
            }
            // then, if this fragment is fully resolved, check to see if any nodes are waiting for this fragment
            if ((ti.WaitingOnFragments?.Count ?? 0) == 0)
            {
                if (_todo != null)
                {
                    var count = _todo.Count;
                    for (var i = 0; i < count; i++)
                    {
                        var todo = _todo[i];
                        if (todo.WaitingOnFragments.Remove(fragmentName))
                        {
                            todo.AnyAuthenticated |= ti.AnyAuthenticated;
                            todo.AnyAnonymous |= ti.AnyAnonymous;
                            if (todo.WaitingOnFragments.Count == 0)
                            {
                                _todo.RemoveAt(i);
                                count--;
                                if (todo.AnyAuthenticated || !todo.AnyAnonymous)
                                {
                                    Validate(todo.ValidationInfo);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates authorization rules for the schema.
        /// Returns a value indicating if validation was successful.
        /// </summary>
        public virtual bool ValidateSchema(ValidationContext context)
            => Validate(context.Schema, null, context);

        /// <summary>
        /// Validate a node that is current within the context.
        /// </summary>
        private bool Validate(IProvideMetadata obj, ASTNode? node, ValidationContext context)
            => Validate(BuildValidationInfo(obj, node, context));

        private static ValidationInfo BuildValidationInfo(IProvideMetadata obj, ASTNode? node, ValidationContext context)
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
        /// <param name="Context">The validaion context; but <see cref="ValidationContext.TypeInfo"/> may not be applicable for node being validated.</param>
        /// <param name="ParentFieldType">For graph types other than operations, the field where this type was referenced; for query arguments, the field to which this argument belongs.</param>
        /// <param name="ParentGraphType">For graph types, the graph type for the field where this type was referenced; for field types, the graph type to which this field belongs; for query arguments, the graph type for the field to which this argument belongs.</param>
        public record struct ValidationInfo(IProvideMetadata Obj, ASTNode? Node, IFieldType? ParentFieldType, IGraphType? ParentGraphType, ValidationContext Context);

        /// <summary>
        /// Validates authorization rules for the specified schema, graph, field or query argument.
        /// Does not consider <see cref="AuthorizationExtensions.IsAnonymousAllowed(IProvideMetadata)"/>.
        /// Returns a value indicating if validation was successful for this node.
        /// </summary>
        protected virtual bool Validate(ValidationInfo info)
        {
            bool requiresAuthorization = info.Obj.IsAuthorizationRequired();
            if (!requiresAuthorization)
                return true;

            var success = true;
            var policies = info.Obj.GetPolicies();
            if (policies?.Count > 0)
            {
                requiresAuthorization = false;
                _policyResults ??= new Dictionary<string, AuthorizationResult>();
                foreach (var policy in policies)
                {
                    if (!_policyResults.TryGetValue(policy, out var result))
                    {
                        result = AuthorizePolicy(policy);
                        _policyResults.Add(policy, result);
                    }
                    if (!result.Succeeded)
                    {
                        HandleNodeNotInPolicy(info, policy, result);
                        success = false;
                    }
                }
            }

            var roles = info.Obj.GetRoles();
            if (roles?.Count > 0)
            {
                requiresAuthorization = false;
                _roleResults ??= new Dictionary<string, bool>();
                foreach (var role in roles)
                {
                    if (!_roleResults.TryGetValue(role, out var result))
                    {
                        result = AuthorizeRole(role);
                        _roleResults.Add(role, result);
                    }
                    if (result)
                        goto PassRoles;
                }
                HandleNodeNotInRoles(info, roles);
                success = false;
            }
        PassRoles:

            if (requiresAuthorization)
            {
                if (!Authorize())
                {
                    HandleNodeNotAuthorized(info);
                    success = false;
                }
            }

            return success;
        }

        /// <inheritdoc cref="IIdentity.IsAuthenticated"/>
        protected virtual bool Authorize()
            => _userIsAuthenticated;

        /// <inheritdoc cref="ClaimsPrincipal.IsInRole(string)"/>
        protected virtual bool AuthorizeRole(string role)
            => ClaimsPrincipal.IsInRole(role);

        /// <inheritdoc cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, string)"/>
        protected virtual AuthorizationResult AuthorizePolicy(string policy)
            => AuthorizePolicyAsync(policy).GetAwaiter().GetResult();

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
        /// Adds a error to the validation context indicating that the user is not a member of any of
        /// the roles required by this graph, field or query argument.
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

        /// <inheritdoc cref="AuthorizationServiceExtensions.AuthorizeAsync(IAuthorizationService, ClaimsPrincipal, string)"/>
        protected virtual Task<AuthorizationResult> AuthorizePolicyAsync(string policy)
            => AuthorizationService.AuthorizeAsync(ClaimsPrincipal, policy);
    }
}

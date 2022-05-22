using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Server.Transports.AspNetCore;

/// <inheritdoc cref="AuthorizationValidationRule"/>
public abstract partial class AuthorizationVisitorBase : INodeVisitor
{
    /// <inheritdoc cref="AuthorizationVisitorBase"/>
    public AuthorizationVisitorBase(ValidationContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        _fragmentDefinitionsToCheck = context.GetRecursivelyReferencedFragments(context.Operation);
    }

    private bool _checkTree; // used to skip processing fragments or operations that do not apply
    private ASTNode? _checkUntil;
    private readonly List<GraphQLFragmentDefinition>? _fragmentDefinitionsToCheck; // contains a list of fragments to process, or null if none
    private readonly Stack<TypeInfo> _onlyAnonymousSelected = new();
    private Dictionary<string, TypeInfo>? _fragments;
    private List<TodoInfo>? _todo;

    /// <inheritdoc/>
    public virtual void Enter(ASTNode node, ValidationContext context)
    {
        // if the node is the selected operation, or if it is a fragment referenced by the current operation,
        // then enable authorization checks on decendant nodes (_checkTree = true)
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
                // if a directive indicates to skip this field, skip authorization checks until Leave() is called for this node
                if (SkipField(fieldNode, context))
                {
                    _checkTree = false;
                    _checkUntil = node;
                }
                else
                {
                    var field = context.TypeInfo.GetFieldDef();
                    // Note: 'field' might be null here if no match was found in the schema (which causes a different validation error).
                    // Also, skip processing for __typeName entirely; do not consider it an anonymous field or
                    // a field that would require authentication for the type -- in this manner, a selection for
                    // only __typename will require authentication, but a selection for __typename and an anonymous
                    // field will not.
                    if (field != null && field != context.Schema.TypeNameMetaFieldType)
                    {
                        // If the field is marked as AllowAnonymous, record that we have encountered a field for this type
                        // which is anonymous; if it is not, record that we have encountered a field for this type
                        // which will require the type to be authenticated.
                        // Also, __type and __schema are implicitly marked as AllowAnonymous; the schema can be marked
                        // with authorization requirements if introspection queries are to be disallowed.
                        var fieldAnonymousAllowed = field.IsAnonymousAllowed() || field == context.Schema.TypeMetaFieldType || field == context.Schema.SchemaMetaFieldType;
                        var ti = _onlyAnonymousSelected.Peek();
                        if (fieldAnonymousAllowed)
                            ti.AnyAnonymous = true;
                        else
                            ti.AnyAuthenticated = true;

                        // Fields, unlike types, are validated immediately.
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
                // if the type already requires authentication, it doesn't matter if the fragment fields
                // are marked as anonymous or not. (note that fragment fields will still get authenticated)
                if (!ti.AnyAuthenticated)
                {
                    // check processed fragments to see if the specified fragment has already been processed;
                    // if so, copy the fragment information in here; otherwise mark this type as being dependent
                    // on fragment fields
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
            // if we are within a field skipped by a directive, resume auth checks at the appropriate time
            if (_checkUntil == node)
            {
                _checkTree = true;
                _checkUntil = null;
            }
            // in any case if this tree is not being checked (not the selected operation or not a fragment spread in use),
            // then return (no auth checks)
            return;
        }
        if (node == context.Operation)
        {
            _checkTree = false;
            PopAndProcess();
        }
        else if (node is GraphQLFragmentDefinition fragmentDefinition)
        {
            // once a fragment is done being processed, apply it to all types waiting on fragment checks,
            // and process checks for types that are not waiting on any fragments
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

        // pop the current type info, and validate the type if it does not contain only fields marked
        // with AllowAnonymous (assuming it is not waiting on fragments)
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

    // note: having TypeInfo a class causes a heap allocation for each node; struct is possible
    // but requires a lot of changes to the code; todo in separate PR
    private class TypeInfo
    {
        public bool AnyAuthenticated;
        public bool AnyAnonymous;
        public List<string>? WaitingOnFragments;
    }

    // an allocation for TodoInfo only occurs when a field references a fragment that has not
    // yet been encountered
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
}

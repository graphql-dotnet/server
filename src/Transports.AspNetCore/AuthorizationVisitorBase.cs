namespace GraphQL.Server.Transports.AspNetCore;

/// <inheritdoc cref="AuthorizationValidationRule"/>
public abstract partial class AuthorizationVisitorBase : INodeVisitor
{
    /// <inheritdoc cref="AuthorizationVisitorBase"/>
    public AuthorizationVisitorBase(ValidationContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        _fragmentDefinitionsToCheck = GetRecursivelyReferencedUsedFragments(context);
    }

    private bool _checkTree; // used to skip processing fragments or operations that do not apply
    private ASTNode? _checkUntil; // used to resume processing after a skipped field (skipped by a directive)
    private readonly List<GraphQLFragmentDefinition>? _fragmentDefinitionsToCheck; // contains a list of fragments to process, or null if none
    private readonly Stack<TypeInfo> _onlyAnonymousSelected = new();
    private Dictionary<string, TypeInfo>? _fragments;
    private List<TodoInfo>? _todos;

    /// <inheritdoc/>
    public virtual async ValueTask EnterAsync(ASTNode node, ValidationContext context)
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
            // if a directive indicates to skip this node, skip authorization checks until Leave() is called for this node
            if (SkipNode(node, context))
            {
                _checkTree = false;
                _checkUntil = node;
            }
            else if (node is GraphQLField)
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
                    var ti = _onlyAnonymousSelected.Pop();
                    if (fieldAnonymousAllowed)
                        ti.AnyAnonymous = true;
                    else
                        ti.AnyAuthenticated = true;
                    _onlyAnonymousSelected.Push(ti);

                    // Fields, unlike types, are validated immediately.
                    if (!fieldAnonymousAllowed)
                    {
                        await ValidateAsync(field, node, context);
                    }
                }

                // prep for descendants, if any
                _onlyAnonymousSelected.Push(new());
            }
            else if (node is GraphQLFragmentSpread fragmentSpread)
            {
                var ti = _onlyAnonymousSelected.Pop();
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
                _onlyAnonymousSelected.Push(ti);
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
                        await ValidateAsync(arg, node, context);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual async ValueTask LeaveAsync(ASTNode node, ValidationContext context)
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
            await PopAndProcessAsync();
        }
        else if (node is GraphQLFragmentDefinition fragmentDefinition)
        {
            // once a fragment is done being processed, apply it to all types waiting on fragment checks,
            // and process checks for types that are not waiting on any fragments
            _checkTree = false;
            var fragmentName = fragmentDefinition.FragmentName.Name.StringValue;
            var ti = _onlyAnonymousSelected.Pop();
            await RecursiveResolveAsync(fragmentName, ti, context);
            _fragments ??= new();
            _fragments.TryAdd(fragmentName, ti);
        }
        else if (_checkTree && node is GraphQLField)
        {
            await PopAndProcessAsync();
        }

        // pop the current type info, and validate the type if it does not contain only fields marked
        // with AllowAnonymous (assuming it is not waiting on fragments)
        async ValueTask PopAndProcessAsync()
        {
            var info = _onlyAnonymousSelected.Pop();
            var type = context.TypeInfo.GetLastType()?.GetNamedType();
            if (type == null)
                return;
            if (info.AnyAuthenticated || (!info.AnyAnonymous && (info.WaitingOnFragments?.Count ?? 0) == 0))
            {
                await ValidateAsync(type, node, context);
            }
            else if (info.WaitingOnFragments?.Count > 0)
            {
                _todos ??= new();
                _todos.Add(new(BuildValidationInfo(node, type, context), info));
            }
        }
    }

    /// <summary>
    /// Indicates if the specified node should skip authentication processing.
    /// Default implementation looks at @skip and @include directives only.
    /// </summary>
    protected virtual bool SkipNode(ASTNode node, ValidationContext context)
    {
        // according to GraphQL spec, directives with the same name may be defined so long as they cannot be
        // placed on the same node types as other directives with the same name; so here we verify that the
        // node is a field, fragment spread, or inline fragment, the only nodes allowed by the built-in @skip
        // and @include directives
        if (node is not GraphQLField && node is not GraphQLFragmentSpread && node is not GraphQLInlineFragment)
            return false;

        var directivesNode = (IHasDirectivesNode)node;

        var skipDirective = directivesNode.Directives?.FirstOrDefault(x => x.Name == "skip");
        if (skipDirective != null)
        {
            var value = GetDirectiveValue(skipDirective, context, false);
            if (value)
                return true;
        }

        var includeDirective = directivesNode.Directives?.FirstOrDefault(x => x.Name == "include");
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

    /// <summary>
    /// Runs when a fragment is added or updated; the fragment might not be waiting on any
    /// other fragments, or it still might be.
    /// </summary>
    private async ValueTask RecursiveResolveAsync(string fragmentName, TypeInfo ti, ValidationContext context)
    {
        // first see if any other fragments are waiting on this fragment
        if (_fragments != null)
        {
        Retry:
            foreach (var fragment in _fragments)
            {
                var ti2 = fragment.Value;
                if (ti2.WaitingOnFragments != null && ti2.WaitingOnFragments.Remove(fragmentName))
                {
                    ti2.AnyAuthenticated |= ti.AnyAuthenticated;
                    ti2.AnyAnonymous |= ti.AnyAnonymous;
                    _fragments[fragment.Key] = ti2;
                    await RecursiveResolveAsync(fragment.Key, ti2, context);
                    goto Retry; // modifying a collection at runtime is not supported
                }
            }
        }
        // then, if this fragment is fully resolved, check to see if any nodes are waiting for this fragment
        if ((ti.WaitingOnFragments?.Count ?? 0) == 0)
        {
            if (_todos != null)
            {
                var count = _todos.Count;
                for (var i = 0; i < count; i++)
                {
                    var todo = _todos[i];
                    if (todo.WaitingOnFragments.Remove(fragmentName))
                    {
                        todo.AnyAuthenticated |= ti.AnyAuthenticated;
                        todo.AnyAnonymous |= ti.AnyAnonymous;
                        if (todo.WaitingOnFragments.Count == 0)
                        {
                            _todos.RemoveAt(i);
                            count--;
                            if (todo.AnyAuthenticated || !todo.AnyAnonymous)
                            {
                                await ValidateAsync(todo.ValidationInfo);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Stores information about the current graph type being examined
    /// to know if all selected fields have been marked with
    /// <see cref="GraphQL.AuthorizationExtensions.AllowAnonymous{TMetadataProvider}(TMetadataProvider)">AllowAnonymous</see>,
    /// in which case authentication checks are skipped for the current graph type.
    /// </summary>
    private struct TypeInfo
    {
        /// <summary>
        /// Indicates if any fields have been selected for the graph type which require authentication.
        /// This includes any fields which are not marked with
        /// <see cref="GraphQL.AuthorizationExtensions.AllowAnonymous{TMetadataProvider}(TMetadataProvider)">AllowAnonymous</see>.
        /// Does not include introspection fields.
        /// </summary>
        public bool AnyAuthenticated;

        /// <summary>
        /// Indicates if any fields have been selected for the graph type which are marked with
        /// <see cref="GraphQL.AuthorizationExtensions.AllowAnonymous{TMetadataProvider}(TMetadataProvider)">AllowAnonymous</see>.
        /// Does not include introspection fields.
        /// </summary>
        public bool AnyAnonymous;

        /// <summary>
        /// A list of fragments referenced in the selection set which have not yet been encountered while
        /// walking the document nodes.
        /// </summary>
        public List<string>? WaitingOnFragments;
    }

    /// <summary>
    /// Stores information about a graph type containing fragment(s) which have not yet
    /// been encountered while walking the document nodes.
    /// <br/><br/>
    /// Once the fragments have all been encountered, authentication checks occur if necessary for the
    /// graph type -- specifically, if any authenticated fields were selected, or if no anonymous fields
    /// were selected.
    /// </summary>
    private class TodoInfo
    {
        /// <inheritdoc cref="ValidationInfo"/>
        public readonly ValidationInfo ValidationInfo;

        /// <inheritdoc cref="TypeInfo.AnyAuthenticated"/>
        public bool AnyAuthenticated;

        /// <inheritdoc cref="TypeInfo.AnyAnonymous"/>
        public bool AnyAnonymous;

        /// <inheritdoc cref="TypeInfo.WaitingOnFragments"/>
        public readonly List<string> WaitingOnFragments;

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

using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;

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
}

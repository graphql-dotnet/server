#nullable enable

using System.Security.Claims;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;
using GraphQLParser.Visitors;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// GraphQL authorization validation rule which integrates to ASP.NET Core authorization mechanism.
    /// For more information see https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction.
    /// </summary>
    public class AuthorizationValidationRule : IValidationRule
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;
        private readonly IAuthorizationErrorMessageBuilder _messageBuilder;

        /// <summary>
        /// Creates an instance of <see cref="AuthorizationValidationRule"/>.
        /// </summary>
        /// <param name="authorizationService"> ASP.NET Core <see cref="IAuthorizationService"/> to authorize against. </param>
        /// <param name="claimsPrincipalAccessor"> The <see cref="IClaimsPrincipalAccessor"/> which provides the <see cref="ClaimsPrincipal"/> for authorization. </param>
        /// <param name="messageBuilder">The <see cref="IAuthorizationErrorMessageBuilder"/> which is used to generate the message for an <see cref="AuthorizationError"/>. </param>
        public AuthorizationValidationRule(
            IAuthorizationService authorizationService,
            IClaimsPrincipalAccessor claimsPrincipalAccessor,
            IAuthorizationErrorMessageBuilder messageBuilder)
        {
            _authorizationService = authorizationService;
            _claimsPrincipalAccessor = claimsPrincipalAccessor;
            _messageBuilder = messageBuilder;
        }

        private bool ShouldBeSkipped(GraphQLOperationDefinition actualOperation, ValidationContext context)
        {
            if (context.Document.OperationsCount() <= 1)
            {
                return false;
            }

            int i = 0;
            do
            {
                var ancestor = context.TypeInfo.GetAncestor(i++);

                if (ancestor == actualOperation)
                {
                    return false;
                }

                if (ancestor == context.Document)
                {
                    return true;
                }

                if (ancestor is GraphQLFragmentDefinition fragment)
                {
                    //TODO: may be rewritten completely later
                    var c = new FragmentBelongsToOperationVisitorContext(fragment);
                    _visitor.VisitAsync(actualOperation, c).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                    return !c.Found;
                }
            } while (true);
        }

        private sealed class FragmentBelongsToOperationVisitorContext : IASTVisitorContext
        {
            public FragmentBelongsToOperationVisitorContext(GraphQLFragmentDefinition fragment)
            {
                Fragment = fragment;
            }

            public GraphQLFragmentDefinition Fragment { get; }

            public bool Found { get; set; }

            public CancellationToken CancellationToken => default;
        }

        private static readonly FragmentBelongsToOperationVisitor _visitor = new();

        private sealed class FragmentBelongsToOperationVisitor : ASTVisitor<FragmentBelongsToOperationVisitorContext>
        {
            protected override ValueTask VisitFragmentSpreadAsync(GraphQLFragmentSpread fragmentSpread, FragmentBelongsToOperationVisitorContext context)
            {
                context.Found = context.Fragment.FragmentName.Name == fragmentSpread.FragmentName.Name;
                return default;
            }

            public override ValueTask VisitAsync(ASTNode? node, FragmentBelongsToOperationVisitorContext context)
            {
                return context.Found ? default : base.VisitAsync(node, context);
            }
        }

        /// <inheritdoc />
        public async ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
        {
            await AuthorizeAsync(null, context.Schema, context, null);
            var operationType = OperationType.Query;

            // this could leak info about hidden fields or types in error messages
            // it would be better to implement a filter on the Schema so it
            // acts as if they just don't exist vs. an auth denied error
            // - filtering the Schema is not currently supported
            // TODO: apply ISchemaFilter - context.Schema.Filter.AllowXXX
            return new NodeVisitors(
                new MatchingNodeVisitor<GraphQLOperationDefinition>((astType, context) =>
                {
                    if (context.Document.OperationsCount() > 1 && astType.Name != context.Operation.Name)
                    {
                        return;
                    }

                    operationType = astType.Operation;

                    var type = context.TypeInfo.GetLastType();
                    AuthorizeAsync(astType, type, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                }),

                new MatchingNodeVisitor<GraphQLObjectField>((objectFieldAst, context) =>
                {
                    if (context.TypeInfo.GetArgument()?.ResolvedType?.GetNamedType() is IComplexGraphType argumentType && !ShouldBeSkipped(context.Operation, context))
                    {
                        var fieldType = argumentType.GetField(objectFieldAst.Name);
                        AuthorizeAsync(objectFieldAst, fieldType, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                    }
                }),

                new MatchingNodeVisitor<GraphQLField>((fieldAst, context) =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();

                    if (fieldDef == null || ShouldBeSkipped(context.Operation, context))
                        return;

                    // check target field
                    AuthorizeAsync(fieldAst, fieldDef, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                    // check returned graph type
                    AuthorizeAsync(fieldAst, fieldDef.ResolvedType?.GetNamedType(), context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                }),

                new MatchingNodeVisitor<GraphQLVariable>((variableRef, context) =>
                {
                    if (context.TypeInfo.GetArgument()?.ResolvedType?.GetNamedType() is not IComplexGraphType variableType || ShouldBeSkipped(context.Operation, context))
                        return;

                    AuthorizeAsync(variableRef, variableType, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this;

                    // Check each supplied field in the variable that exists in the variable type.
                    // If some supplied field does not exist in the variable type then some other
                    // validation rule should check that but here we should just ignore that
                    // "unknown" field.
                    if (context.Variables != null &&
                        context.Variables.TryGetValue(variableRef.Name.StringValue, out object? input) && //ISSUE:allocation
                        input is Dictionary<string, object> fieldsValues)
                    {
                        foreach (var field in variableType.Fields)
                        {
                            if (fieldsValues.ContainsKey(field.Name))
                            {
                                AuthorizeAsync(variableRef, field, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this;
                            }
                        }
                    }
                })
            );
        }

        private async Task AuthorizeAsync(ASTNode? node, IProvideMetadata? provider, ValidationContext context, OperationType? operationType)
        {
            var policyNames = provider?.GetPolicies();

            if (policyNames?.Count == 1)
            {
                // small optimization for the single policy - no 'new List<>()', no 'await Task.WhenAll()'
                var authorizationResult = await _authorizationService.AuthorizeAsync(_claimsPrincipalAccessor.GetClaimsPrincipal(context), policyNames[0]);
                if (!authorizationResult.Succeeded)
                    AddValidationError(node, context, operationType, authorizationResult);
            }
            else if (policyNames?.Count > 1)
            {
                var claimsPrincipal = _claimsPrincipalAccessor.GetClaimsPrincipal(context);
                var tasks = new List<Task<AuthorizationResult>>(policyNames.Count);
                foreach (string policyName in policyNames)
                {
                    var task = _authorizationService.AuthorizeAsync(claimsPrincipal, policyName);
                    tasks.Add(task);
                }

                var authorizationResults = await Task.WhenAll(tasks);

                foreach (var result in authorizationResults)
                {
                    if (!result.Succeeded)
                        AddValidationError(node, context, operationType, result);
                }
            }
        }

        /// <summary>
        /// Adds an authorization failure error to the document response
        /// </summary>
        protected virtual void AddValidationError(ASTNode? node, ValidationContext context, OperationType? operationType, AuthorizationResult result)
        {
            string message = _messageBuilder.GenerateMessage(operationType, result);
            context.ReportError(new AuthorizationError(node, context, message, result, operationType));
        }
    }
}

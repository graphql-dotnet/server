using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// GraphQL authorization validation rule which integrates to ASP.NET Core authorization mechanism.
    /// For more information see https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction.
    /// </summary>
    public class AuthorizationValidationRule : IValidationRule
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Creates an instance of <see cref="AuthorizationValidationRule"/>.
        /// </summary>
        /// <param name="authorizationService"> ASP.NET Core <see cref="IAuthorizationService"/> to authorize against. </param>
        /// <param name="httpContextAccessor"> ASP.NET Core <see cref="IHttpContextAccessor"/> to take user (HttpContext.User) from. </param>
        public AuthorizationValidationRule(IAuthorizationService authorizationService, IHttpContextAccessor httpContextAccessor)
        {
            _authorizationService = authorizationService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            return Task.FromResult((INodeVisitor)new EnterLeaveListener(_ =>
            {
                AuthorizeAsync(null, context.Schema, context, null).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this

                var operationType = OperationType.Query;

                // this could leak info about hidden fields or types in error messages
                // it would be better to implement a filter on the Schema so it
                // acts as if they just don't exist vs. an auth denied error
                // - filtering the Schema is not currently supported

                _.Match<Operation>(astType =>
                {
                    operationType = astType.OperationType;

                    var type = context.TypeInfo.GetLastType();
                    AuthorizeAsync(astType, type, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                });

                _.Match<ObjectField>(objectFieldAst =>
                {
                    if (context.TypeInfo.GetArgument().ResolvedType.GetNamedType() is IComplexGraphType argumentType)
                    {
                        var fieldType = argumentType.GetField(objectFieldAst.Name);
                        AuthorizeAsync(objectFieldAst, fieldType, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                    }
                });

                _.Match<Field>(fieldAst =>
                {
                    var fieldDef = context.TypeInfo.GetFieldDef();
                    if (fieldDef == null)
                    {
                        return;
                    }

                    // check target field
                    AuthorizeAsync(fieldAst, fieldDef, context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                    // check returned graph type
                    AuthorizeAsync(fieldAst, fieldDef.ResolvedType.GetNamedType(), context, operationType).GetAwaiter().GetResult(); // TODO: need to think of something to avoid this
                });
            }));
        }

        private async Task AuthorizeAsync(INode node, IProvideMetadata type, ValidationContext context, OperationType? operationType)
        {
            var policyNames = type?.GetPolicies();

            if (policyNames?.Count == 1)
            {
                // small optimization for the single policy - no 'new List<>()', no 'await Task.WhenAll()'
                var authorizationResult = await _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, policyNames[0]);
                AddValidationError(node, context, operationType, authorizationResult);
            }
            else if (policyNames?.Count > 1)
            {
                var tasks = new List<Task<AuthorizationResult>>(policyNames.Count);
                foreach (string policyName in policyNames)
                {
                    var task = _authorizationService.AuthorizeAsync(_httpContextAccessor.HttpContext.User, policyName);
                    tasks.Add(task);
                }

                var authorizationResults = await Task.WhenAll(tasks);

                foreach (var result in authorizationResults)
                {
                    AddValidationError(node, context, operationType, result);
                }
            }

            static void AddValidationError(INode node, ValidationContext context, OperationType? operationType, AuthorizationResult result)
            {
                if (!result.Succeeded)
                {
                    var error = new StringBuilder("You are not authorized to run this ")
                                .Append(GetOperationType(operationType))
                                .Append(".");

                    foreach (var failure in result.Failure.FailedRequirements)
                    {
                        AppendFailureLine(error, failure);
                    }

                    context.ReportError(new ValidationError(context.OriginalQuery, "6.1.1", error.ToString(), node == null ? Array.Empty<INode>() : new INode[] { node })
                    {
                        Code = "authorization",
                    });
                }
            }
        }

        private static string GetOperationType(OperationType? operationType)
        {
            return operationType switch
            {
                OperationType.Query => "query",
                OperationType.Mutation => "mutation",
                OperationType.Subscription => "subscription",
                _ => "operation",
            };
        }

        private static void AppendFailureLine(StringBuilder error, IAuthorizationRequirement authorizationRequirement)
        {
            error.AppendLine();

            switch (authorizationRequirement)
            {
                case ClaimsAuthorizationRequirement claimsAuthorizationRequirement:
                    error.Append("Required claim '");
                    error.Append(claimsAuthorizationRequirement.ClaimType);
                    if (claimsAuthorizationRequirement.AllowedValues == null || !claimsAuthorizationRequirement.AllowedValues.Any())
                    {
                        error.Append("' is not present.");
                    }
                    else
                    {
                        error.Append("' with any value of '");
                        error.Append(string.Join(", ", claimsAuthorizationRequirement.AllowedValues));
                        error.Append("' is not present.");
                    }
                    break;

                case DenyAnonymousAuthorizationRequirement _:
                    error.Append("The current user must be authenticated.");
                    break;

                case NameAuthorizationRequirement nameAuthorizationRequirement:
                    error.Append("The current user name must match the name '");
                    error.Append(nameAuthorizationRequirement.RequiredName);
                    error.Append("'.");
                    break;

                case OperationAuthorizationRequirement operationAuthorizationRequirement:
                    error.Append("Required operation '");
                    error.Append(operationAuthorizationRequirement.Name);
                    error.Append("' was not present.");
                    break;

                case RolesAuthorizationRequirement rolesAuthorizationRequirement:
                    if (rolesAuthorizationRequirement.AllowedRoles == null || !rolesAuthorizationRequirement.AllowedRoles.Any())
                    {
                        // This should never happen.
                        error.Append("Required roles are not present.");
                    }
                    else
                    {
                        error.Append("Required roles '");
                        error.Append(string.Join(", ", rolesAuthorizationRequirement.AllowedRoles));
                        error.Append("' are not present.");
                    }
                    break;

                case AssertionRequirement _:
                default:
                    error.Append("Requirement '");
                    error.Append(authorizationRequirement.GetType().Name);
                    error.Append("' was not satisfied.");
                    break;
            }
        }
    }
}

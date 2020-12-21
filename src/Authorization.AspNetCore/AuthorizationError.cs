using System;
using System.Linq;
using System.Text;
using GraphQL.Language.AST;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// An error that represents an authorization failure while parsing the document
    /// </summary>
    public class AuthorizationError : ValidationError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationError"/> class for a specified authorization result
        /// </summary>
        public AuthorizationError(INode node, ValidationContext context, OperationType? operationType, AuthorizationResult result)
            : this(node, context, GenerateMessage(operationType, result), result)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationError"/> class for a specified authorization result with a specific error message
        /// </summary>
        public AuthorizationError(INode node, ValidationContext context, string message, AuthorizationResult result)
            : base(context.OriginalQuery, "6.1.1", message, node == null ? Array.Empty<INode>() : new INode[] { node })
        {
            Code = "authorization";
            AuthorizationResult = result;
        }

        /// <summary>
        /// Returns the result of the Asp.Net Core authorization request
        /// </summary>
        public virtual AuthorizationResult AuthorizationResult { get; }

        private static string GenerateMessage(OperationType? operationType, AuthorizationResult result)
        {
            var error = new StringBuilder("You are not authorized to run this ")
            .Append(GetOperationType(operationType))
            .Append(".");

            foreach (var failure in result.Failure.FailedRequirements)
            {
                AppendFailureLine(error, failure);
            }

            return error.ToString();
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

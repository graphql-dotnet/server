using System.Linq;
using System.Text;
using GraphQL.Language.AST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// The default authorization failure description generator.
    /// </summary>
    public class DefaultAuthorizationFailureDescriptionGenerator: IAuthorizationFailureDescriptionGenerator
    {
        /// <inheritdoc />
        public virtual string GenerateFailureDescription(AuthorizationResult result, OperationType? operationType)
        {
            var error = new StringBuilder("You are not authorized to run this ")
                .Append(GetOperationType(operationType))
                .Append(".");

            foreach (var failure in result.Failure.FailedRequirements)
                AppendFailureLine(error, failure);

            return error.ToString();
        }

        /// <summary>
        /// Transforms the given <paramref name="operationType"/> into a string to be used in a failure description
        /// </summary>
        /// <param name="operationType">The GraphQL operation type</param>
        /// <returns></returns>
        protected virtual string GetOperationType(OperationType? operationType) =>
            operationType switch
            {
                OperationType.Query => "query",
                OperationType.Mutation => "mutation",
                OperationType.Subscription => "subscription",
                _ => "operation",
            };

        /// <summary>
        /// Generates a description for a single failed <see cref="IAuthorizationRequirement"/>
        /// </summary>
        /// <param name="error">The error string builder to accumulate generated text</param>
        /// <param name="authorizationRequirement">the failed authorization requirement</param>
        protected virtual void AppendFailureLine(StringBuilder error, IAuthorizationRequirement authorizationRequirement)
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

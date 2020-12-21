using System.Linq;
using System.Text;
using GraphQL.Language.AST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// The default authorization failure description generator for this library
    /// </summary>
    public class DefaultAuthorizationFailureDescriptionGenerator: IAuthorizationFailureDescriptionGenerator
    {
        /// <inheritdoc />
        public virtual string GenerateFailureDescription(AuthorizationResult result, OperationType? operationType)
        {
            var messageBuilder = new StringBuilder("You are not authorized to run this ")
                .Append(GetOperationType(operationType))
                .Append(".");

            foreach (var failure in result.Failure.FailedRequirements)
                AppendFailureLine(messageBuilder, failure);

            return messageBuilder.ToString();
        }

        /// <summary>
        /// Transforms the given <paramref name="operationType"/> into a string to be used in a failure description
        /// </summary>
        /// <param name="operationType">the GraphQL operation type</param>
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
        /// <param name="messageBuilder">the message string builder</param>
        /// <param name="authorizationRequirement">the failed authorization requirement</param>
        protected virtual void AppendFailureLine(StringBuilder messageBuilder, IAuthorizationRequirement authorizationRequirement)
        {
            messageBuilder.AppendLine();

            switch (authorizationRequirement)
            {
                case ClaimsAuthorizationRequirement claimsAuthorizationRequirement:
                    messageBuilder.Append("Required claim '");
                    messageBuilder.Append(claimsAuthorizationRequirement.ClaimType);
                    if (claimsAuthorizationRequirement.AllowedValues == null || !claimsAuthorizationRequirement.AllowedValues.Any())
                    {
                        messageBuilder.Append("' is not present.");
                    }
                    else
                    {
                        messageBuilder.Append("' with any value of '");
                        messageBuilder.Append(string.Join(", ", claimsAuthorizationRequirement.AllowedValues));
                        messageBuilder.Append("' is not present.");
                    }
                    break;

                case DenyAnonymousAuthorizationRequirement _:
                    messageBuilder.Append("The current user must be authenticated.");
                    break;

                case NameAuthorizationRequirement nameAuthorizationRequirement:
                    messageBuilder.Append("The current user name must match the name '");
                    messageBuilder.Append(nameAuthorizationRequirement.RequiredName);
                    messageBuilder.Append("'.");
                    break;

                case OperationAuthorizationRequirement operationAuthorizationRequirement:
                    messageBuilder.Append("Required operation '");
                    messageBuilder.Append(operationAuthorizationRequirement.Name);
                    messageBuilder.Append("' was not present.");
                    break;

                case RolesAuthorizationRequirement rolesAuthorizationRequirement:
                    if (rolesAuthorizationRequirement.AllowedRoles == null || !rolesAuthorizationRequirement.AllowedRoles.Any())
                    {
                        // This should never happen.
                        messageBuilder.Append("Required roles are not present.");
                    }
                    else
                    {
                        messageBuilder.Append("Required roles '");
                        messageBuilder.Append(string.Join(", ", rolesAuthorizationRequirement.AllowedRoles));
                        messageBuilder.Append("' are not present.");
                    }
                    break;

                case AssertionRequirement _:
                default:
                    messageBuilder.Append("Requirement '");
                    messageBuilder.Append(authorizationRequirement.GetType().Name);
                    messageBuilder.Append("' was not satisfied.");
                    break;
            }
        }
    }
}

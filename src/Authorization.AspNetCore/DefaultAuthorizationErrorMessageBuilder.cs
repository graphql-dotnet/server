using System.Text;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphQL.Server.Authorization.AspNetCore;

/// <summary>
/// Default error message builder implementation.
/// </summary>
[Obsolete("This class will be removed in v8 as revealing authorization requirements may be a security risk; please use ErrorInfoProvider if you require detailed access-denied error messages.")]
public class DefaultAuthorizationErrorMessageBuilder : IAuthorizationErrorMessageBuilder
{
    /// <inheritdoc />
    public virtual string GenerateMessage(OperationType? operationType, AuthorizationResult result)
    {
        if (result.Succeeded)
            return "Success!";

        var error = new StringBuilder();
        AppendFailureHeader(error, operationType);

        if (result.Failure != null)
        {
            foreach (var requirement in result.Failure.FailedRequirements)
            {
                AppendFailureLine(error, requirement);
            }
        }

        return error.ToString();
    }

    private string GetOperationType(OperationType? operationType)
    {
        return operationType switch
        {
            OperationType.Query => "query",
            OperationType.Mutation => "mutation",
            OperationType.Subscription => "subscription",
            _ => "operation",
        };
    }

    /// <inheritdoc />
    public virtual void AppendFailureHeader(StringBuilder errorBuilder, OperationType? operationType)
    {
        errorBuilder
            .Append("You are not authorized to run this ")
            .Append(GetOperationType(operationType))
            .Append('.');
    }

    /// <inheritdoc />
    public virtual void AppendFailureLine(StringBuilder errorBuilder, IAuthorizationRequirement authorizationRequirement)
    {
        errorBuilder.AppendLine();

        switch (authorizationRequirement)
        {
            case ClaimsAuthorizationRequirement claimsAuthorizationRequirement:
                errorBuilder.Append("Required claim '");
                errorBuilder.Append(claimsAuthorizationRequirement.ClaimType);
                if (claimsAuthorizationRequirement.AllowedValues == null || !claimsAuthorizationRequirement.AllowedValues.Any())
                {
                    errorBuilder.Append("' is not present.");
                }
                else
                {
                    errorBuilder.Append("' with any value of '");
                    errorBuilder.Append(string.Join(", ", claimsAuthorizationRequirement.AllowedValues));
                    errorBuilder.Append("' is not present.");
                }
                break;

            case DenyAnonymousAuthorizationRequirement _:
                errorBuilder.Append("The current user must be authenticated.");
                break;

            case NameAuthorizationRequirement nameAuthorizationRequirement:
                errorBuilder.Append("The current user name must match the name '");
                errorBuilder.Append(nameAuthorizationRequirement.RequiredName);
                errorBuilder.Append("'.");
                break;

            case OperationAuthorizationRequirement operationAuthorizationRequirement:
                errorBuilder.Append("Required operation '");
                errorBuilder.Append(operationAuthorizationRequirement.Name);
                errorBuilder.Append("' was not present.");
                break;

            case RolesAuthorizationRequirement rolesAuthorizationRequirement:
                if (rolesAuthorizationRequirement.AllowedRoles == null || !rolesAuthorizationRequirement.AllowedRoles.Any())
                {
                    // This should never happen.
                    errorBuilder.Append("Required roles are not present.");
                }
                else
                {
                    errorBuilder.Append("Required roles '");
                    errorBuilder.Append(string.Join(", ", rolesAuthorizationRequirement.AllowedRoles));
                    errorBuilder.Append("' are not present.");
                }
                break;

            case AssertionRequirement _:
            default:
                errorBuilder.Append("Requirement '");
                errorBuilder.Append(authorizationRequirement.GetType().Name);
                errorBuilder.Append("' was not satisfied.");
                break;
        }
    }
}

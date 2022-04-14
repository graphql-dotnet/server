#nullable enable

using System.Text;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphQL.Server.Authorization.AspNetCore;

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
    public virtual void AppendFailureHeader(StringBuilder error, OperationType? operationType)
    {
        error
            .Append("You are not authorized to run this ")
            .Append(GetOperationType(operationType))
            .Append('.');
    }

    /// <inheritdoc />
    public virtual void AppendFailureLine(StringBuilder error, IAuthorizationRequirement authorizationRequirement)
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

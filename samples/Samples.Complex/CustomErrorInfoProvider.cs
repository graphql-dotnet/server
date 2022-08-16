using System.Text;
using GraphQL.Execution;
using GraphQL.Server.Transports.AspNetCore.Errors;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Samples.Complex;

/// <summary>
/// Custom <see cref="ErrorInfoProvider"/> implementing a dedicated error message for the sample <see cref="IAuthorizationRequirement"/>
/// used in this MS article: https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies
/// </summary>
public class CustomErrorInfoProvider : ErrorInfoProvider
{
    public override ErrorInfo GetInfo(ExecutionError executionError)
    {
        var info = base.GetInfo(executionError);

        if (executionError is AccessDeniedError accessDeniedError)
            info.Message = GetAuthorizationErrorMessage(accessDeniedError);

        return info;
    }

    private string GetAuthorizationErrorMessage(AccessDeniedError error)
    {
        var errorMessage = new StringBuilder();
        errorMessage.Append(error.Message);

        if (error.PolicyAuthorizationResult != null)
        {
            foreach (var failedRequirement in error.PolicyAuthorizationResult.Failure!.FailedRequirements)
            {
                switch (failedRequirement)
                {
                    case MinimumAgeRequirement minimumAgeRequirement:
                        errorMessage.AppendLine();
                        errorMessage.Append("The current user must be at least ");
                        errorMessage.Append(minimumAgeRequirement.MinimumAge);
                        errorMessage.Append(" years old.");
                        break;
                }
            }
        }

        return errorMessage.ToString();
    }
}

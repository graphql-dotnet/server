using GraphQL.Execution;
using GraphQL.Server.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Samples.Server
{
    /// <summary>
    /// custom <see cref="ErrorInfoProvider"/> implementing a dedicated error message for the example <see cref="IAuthorizationRequirement"/>
    /// used in this MS article: https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies
    /// </summary>
    public class CustomErrorInfoProvider: ErrorInfoProvider
    {
        public override ErrorInfo GetInfo(ExecutionError executionError)
        {
            var info = base.GetInfo(executionError);
            info.Message = executionError switch
            {
                AuthorizationError authorizationError => GetAuthorizationErrorMessage(authorizationError),
                _ => info.Message,
            };
            return info;
        }

        private string GetAuthorizationErrorMessage(AuthorizationError error)
        {
            var errorMessage = error.GetErrorStringBuilder();

            foreach (var failedRequirement in error.AuthorizationResult.Failure.FailedRequirements)
            {
                switch (failedRequirement)
                {
                    case MinimumAgeRequirement minimumAgeRequirement:
                        errorMessage.AppendLine();
                        errorMessage.Append("The current user must be at least ");
                        errorMessage.Append(minimumAgeRequirement.MinimumAge);
                        errorMessage.Append(" years old.");
                        break;
                    default:
                        AuthorizationError.AppendFailureLine(errorMessage, failedRequirement);
                        break;
                }
            }

            return errorMessage.ToString();
        }
    }
}

using System.Text;
using GraphQL.Execution;
using GraphQL.Server;
using GraphQL.Server.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace GraphQL.Samples.Server
{
    /// <summary>
    /// Custom <see cref="ErrorInfoProvider"/> implementing a dedicated error message for the sample <see cref="IAuthorizationRequirement"/>
    /// used in this MS article: https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies
    /// </summary>
    public class CustomErrorInfoProvider : DefaultErrorInfoProvider
    {
        private readonly IAuthorizationErrorMessageBuilder _messageBuilder;

        public CustomErrorInfoProvider(IOptions<ErrorInfoProviderOptions> options, IAuthorizationErrorMessageBuilder messageBuilder)
            : base(options)
        {
            _messageBuilder = messageBuilder;
        }

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
            var errorMessage = new StringBuilder();
            _messageBuilder.AppendFailureHeader(errorMessage, error.OperationType);

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
                        _messageBuilder.AppendFailureLine(errorMessage, failedRequirement);
                        break;
                }
            }

            return errorMessage.ToString();
        }
    }
}

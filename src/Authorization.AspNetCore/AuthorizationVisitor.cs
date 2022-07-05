using System.Security.Claims;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace GraphQL.Server.Authorization.AspNetCore;

public partial class AuthorizationValidationRule
{
    /// <inheritdoc/>
    [Obsolete("This class has been replaced by GraphQL.Server.Transports.AspNetCore.AuthorizationValidationRule.AuthorizationVisitor and will be removed in v8.")]
    public class AuthorizationVisitor : Transports.AspNetCore.AuthorizationVisitor
    {
        private readonly IAuthorizationErrorMessageBuilder _messageBuilder;
        private readonly AuthorizationValidationRule _authorizationValidationRule;

        /// <inheritdoc cref="AuthorizationVisitor"/>
        public AuthorizationVisitor(ValidationContext context, ClaimsPrincipal claimsPrincipal, IAuthorizationService authorizationService, IAuthorizationErrorMessageBuilder authorizationErrorMessageBuilder, AuthorizationValidationRule authorizationValidationRule)
            : base(context, claimsPrincipal, authorizationService)
        {
            _messageBuilder = authorizationErrorMessageBuilder;
            _authorizationValidationRule = authorizationValidationRule;
        }

        /// <inheritdoc/>
        protected override void HandleNodeNotAuthorized(ValidationInfo info)
            => ReportError(info, new DenyAnonymousAuthorizationRequirement());

        /// <inheritdoc/>
        protected override void HandleNodeNotInRoles(ValidationInfo info, List<string> roles)
            => ReportError(info, new RolesAuthorizationRequirement(roles));

        /// <inheritdoc/>
        protected override void HandleNodeNotInPolicy(ValidationInfo info, string policy, AuthorizationResult authorizationResult)
            => ReportError(info, authorizationResult);

        private void ReportError(ValidationInfo info, IAuthorizationRequirement authorizationRequirement)
            => ReportError(info, AuthorizationResult.Failed(AuthorizationFailure.Failed(new[] { authorizationRequirement })));

        private void ReportError(ValidationInfo info, AuthorizationResult authorizationResult)
        {
            _authorizationValidationRule.AddValidationError(info.Node, info.Context, info.Context.Operation.Operation, authorizationResult);
        }
    }
}

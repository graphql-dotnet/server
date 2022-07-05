using System.Security.Claims;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Server.Authorization.AspNetCore;

/// <summary>
/// GraphQL authorization validation rule which integrates to ASP.NET Core authorization mechanism.
/// For more information see https://docs.microsoft.com/en-us/aspnet/core/security/authorization/introduction.
/// </summary>
[Obsolete("This class has been replaced by GraphQL.Server.Transports.AspNetCore.AuthorizationValidationRule and will be removed in v8.")]
public partial class AuthorizationValidationRule : IValidationRule
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IClaimsPrincipalAccessor _claimsPrincipalAccessor;
    private readonly IAuthorizationErrorMessageBuilder _messageBuilder;

    /// <summary>
    /// Creates an instance of <see cref="AuthorizationValidationRule"/>.
    /// </summary>
    /// <param name="authorizationService"> ASP.NET Core <see cref="IAuthorizationService"/> to authorize against. </param>
    /// <param name="claimsPrincipalAccessor"> The <see cref="IClaimsPrincipalAccessor"/> which provides the <see cref="ClaimsPrincipal"/> for authorization. </param>
    /// <param name="messageBuilder">The <see cref="IAuthorizationErrorMessageBuilder"/> which is used to generate the message for an <see cref="AuthorizationError"/>. </param>
    public AuthorizationValidationRule(
        IAuthorizationService authorizationService,
        IClaimsPrincipalAccessor claimsPrincipalAccessor,
        IAuthorizationErrorMessageBuilder messageBuilder)
    {
        _authorizationService = authorizationService;
        _claimsPrincipalAccessor = claimsPrincipalAccessor;
        _messageBuilder = messageBuilder;
    }

    /// <inheritdoc/>
    public async ValueTask<INodeVisitor?> ValidateAsync(ValidationContext context)
    {
        var visitor = new AuthorizationVisitor(context, _claimsPrincipalAccessor.GetClaimsPrincipal(context), _authorizationService, _messageBuilder, this);

        // if the schema fails authentication, report the error and do not perform any additional authorization checks.
        return await visitor.ValidateSchemaAsync(context) ? visitor : null;
    }

    /// <summary>
    /// Adds an authorization failure error to the document response
    /// </summary>
    protected virtual void AddValidationError(GraphQLParser.AST.ASTNode? node, ValidationContext context, GraphQLParser.AST.OperationType? operationType, AuthorizationResult result)
    {
        string message = _messageBuilder.GenerateMessage(operationType, result);
        context.ReportError(new AuthorizationError(node, context, message, result, operationType));
    }
}

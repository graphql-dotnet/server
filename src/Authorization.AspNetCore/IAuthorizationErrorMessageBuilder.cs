#nullable enable

using System.Text;
using GraphQL.Language.AST;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Server.Authorization.AspNetCore;

public interface IAuthorizationErrorMessageBuilder
{
    string GenerateMessage(OperationType? operationType, AuthorizationFailure failure);

    /// <summary>
    /// Appends the error message header for this <see cref="AuthorizationError"/> to the provided <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="error">The error message <see cref="StringBuilder"/>.</param>
    /// <param name="operationType">The GraphQL operation type.</param>
    void AppendFailureHeader(StringBuilder error, OperationType? operationType);

    /// <summary>
    /// Appends a description of the failed <paramref name="authorizationRequirement"/> to the supplied <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="error">The <see cref="StringBuilder"/> which is used to compose the error message.</param>
    /// <param name="authorizationRequirement">The failed <see cref="IAuthorizationRequirement"/>.</param>
    void AppendFailureLine(StringBuilder error, IAuthorizationRequirement authorizationRequirement);
}

using System.Text;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Server.Authorization.AspNetCore;

[Obsolete("This class will be removed in v8 as revealing authorization requirements may be a security risk; please use ErrorInfoProvider if you require detailed access-denied error messages.")]
public interface IAuthorizationErrorMessageBuilder
{
    /// <summary>
    /// Generates an authorization error message based on the provided <see cref="AuthorizationResult"/>
    /// </summary>
    /// <param name="operationType">The GraphQL operation type.</param>
    /// <param name="result">The <see cref="AuthorizationResult"/> which is used to generate the message.</param>
    /// <returns>The generated error message.</returns>
    string GenerateMessage(OperationType? operationType, AuthorizationResult result);

    /// <summary>
    /// Appends the error message header to the provided <see cref="StringBuilder"/>.
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

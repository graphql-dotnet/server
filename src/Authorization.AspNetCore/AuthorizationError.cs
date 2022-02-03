#nullable enable

using System;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// An error that represents an authorization failure while parsing the document.
    /// </summary>
    public class AuthorizationError : ValidationError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationError"/> class for a specified authorization result with a specific error message.
        /// </summary>
        public AuthorizationError(ASTNode? node, ValidationContext context, string message, AuthorizationResult result, OperationType? operationType = null)
            : base(context.Document.Source, "6.1.1", message, node == null ? Array.Empty<ASTNode>() : new ASTNode[] { node })
        {
            Code = "authorization";
            AuthorizationResult = result;
            OperationType = operationType;
        }

        /// <summary>
        /// Returns the result of the ASP.NET Core authorization request.
        /// </summary>
        public virtual AuthorizationResult AuthorizationResult { get; }

        /// <summary>
        /// The GraphQL operation type.
        /// </summary>
        public OperationType? OperationType { get; }
    }
}

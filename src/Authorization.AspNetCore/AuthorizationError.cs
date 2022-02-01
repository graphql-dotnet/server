using System;
using GraphQL.Language.AST;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Server.Authorization.AspNetCore
{
    /// <summary>
    /// An error that represents an authorization failure while parsing the document.
    /// </summary>
    public class AuthorizationError : ValidationError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationError"/> class for a specified authorization result.
        /// </summary>
        public AuthorizationError(INode node, ValidationContext context, OperationType? operationType, string message, AuthorizationResult result)
            : this(node, context, message, result)
        {
            OperationType = operationType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationError"/> class for a specified authorization result with a specific error message.
        /// </summary>
        public AuthorizationError(INode node, ValidationContext context, string message, AuthorizationResult result)
            : base(context.Document.OriginalQuery, "6.1.1", message, node == null ? Array.Empty<INode>() : new INode[] { node })
        {
            Code = "authorization";
            AuthorizationResult = result;
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

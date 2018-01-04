using System;
using System.Collections.Generic;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLWebSocketsOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public Func<OperationMessageContext, object> BuildUserContext { get; set; }

        public bool ExposeExceptions { get; set; }

        public IList<IValidationRule> ValidationRules { get; } = new List<IValidationRule>();
    }
}

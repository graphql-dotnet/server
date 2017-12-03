using System;
using GraphQL.Server.Transports.WebSockets.Messages;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQlWebSocketsOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public Func<OperationMessageContext, object> BuildUserContext { get; set; }
    }
}

namespace GraphQL.Server.Transports.WebSockets
{
    public enum WebSocketsSubprotocol
    {
        /// <summary>
        /// Old Protocol
        /// https://github.com/apollographql/subscriptions-transport-ws/blob/master/PROTOCOL.md
        /// </summary>
        GraphQLWs,

        /// <summary>
        /// New Protocol
        /// https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md
        /// </summary>
        GraphQLTransportWs,
    }
}

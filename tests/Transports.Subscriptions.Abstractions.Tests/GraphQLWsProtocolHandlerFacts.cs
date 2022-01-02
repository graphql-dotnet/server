namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    internal class GraphQLWsProtocolHandlerFacts : BaseProtocolHandlerFacts
    {
        public GraphQLWsProtocolHandlerFacts():
            base(MessageType.GQL_START, MessageType.GQL_DATA)
        {
        }
    }
}

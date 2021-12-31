namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class GraphQLTransportWsProtocolHandlerFacts : BaseProtocolHandlerFacts
    {
        public GraphQLTransportWsProtocolHandlerFacts()
            : base(MessageType.GQL_SUBSRIBE, MessageType.GQL_NEXT)
        {
        }
    }
}

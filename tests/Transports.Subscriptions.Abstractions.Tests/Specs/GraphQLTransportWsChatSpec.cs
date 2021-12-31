namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests.Specs
{
    public class GraphQLTransportWsChatSpec : BaseChatSpec
    {
        public GraphQLTransportWsChatSpec()
            : base(MessageType.GQL_SUBSRIBE, MessageType.GQL_NEXT)
        {
        }
    }
}

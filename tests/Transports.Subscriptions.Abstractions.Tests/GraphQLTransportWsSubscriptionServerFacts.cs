namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class GraphQLTransportWsSubscriptionServerFacts : BaseSubscriptionServerFacts
    {
        public GraphQLTransportWsSubscriptionServerFacts()
            : base(MessageType.GQL_NEXT)
        {
        }
    }
}

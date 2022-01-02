namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class GraphQLTransportWsSubscriptionManagerFacts : BaseSubscriptionManagerFacts
    {
        public GraphQLTransportWsSubscriptionManagerFacts()
            : base(MessageType.GQL_NEXT)
        {
        }
    }
}

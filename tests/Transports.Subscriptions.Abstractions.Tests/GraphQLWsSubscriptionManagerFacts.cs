namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class GraphQLWsSubscriptionManagerFacts : BaseSubscriptionManagerFacts
    {
        public GraphQLWsSubscriptionManagerFacts()
            : base(MessageType.GQL_DATA)
        {
        }
    }
}

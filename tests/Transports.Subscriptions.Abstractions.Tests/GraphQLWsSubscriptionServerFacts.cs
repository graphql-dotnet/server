namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class GraphQLWsSubscriptionServerFacts : BaseSubscriptionServerFacts
    {
        public GraphQLWsSubscriptionServerFacts()
            : base(MessageType.GQL_DATA)
        {
        }
    }
}

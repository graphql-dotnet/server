namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests.Specs
{
    public class GraphQLWsChatSpec : BaseChatSpec
    {
        public GraphQLWsChatSpec()
            : base(MessageType.GQL_START, MessageType.GQL_DATA)
        {
        }
    }
}

using System.Threading.Tasks;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class SubscriptionServerFacts
    {
        private readonly TestableSubscriptionTransport _transport;
        private readonly SubscriptionServer _sut;

        public SubscriptionServerFacts()
        {
            _transport = new TestableSubscriptionTransport();
            _sut = new SubscriptionServer(_transport);
        }
        [Fact]
        public async Task Receive_init_message()
        {
            /* Given */
            var expected = new OperationMessage()
            {
                Type = MessageTypeConstants.GQL_CONNECTION_INIT
            };
            _transport.AddMessageToRead(expected);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageTypeConstants.GQL_CONNECTION_ACK);
        }

    }
}
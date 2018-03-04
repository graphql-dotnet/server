using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Subscription;
using NSubstitute;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class SubscriptionServerFacts
    {
        public SubscriptionServerFacts()
        {
            _transport = new TestableSubscriptionTransport();
            _subscriptionExecuter = Substitute.For<ISubscriptionExecuter>();
            _subscriptionExecuter.SubscribeAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult()
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                    {
                        {"1", Substitute.For<IObservable<ExecutionResult>>()}
                    }
                });
            _subscriptionManager = new SubscriptionManager(_subscriptionExecuter);
            _sut = new SubscriptionServer(_transport, _subscriptionManager);
        }

        private readonly TestableSubscriptionTransport _transport;
        private readonly SubscriptionServer _sut;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly ISubscriptionExecuter _subscriptionExecuter;

        [Fact]
        public async Task Receive_init()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_INIT
            };
            _transport.AddMessageToRead(expected);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Contains(_transport.WrittenMessages,
                message => message.Type == MessageType.GQL_CONNECTION_ACK);
        }

        [Fact]
        public async Task Receive_start()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = new OperationMessagePayload
                {
                    Query = "query"
                }
            };
            _transport.AddMessageToRead(expected);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Single(_sut.Subscriptions, sub => sub.Id == expected.Id);
        }

        [Fact]
        public async Task Receive_stop()
        {
            /* Given */
            var subscribe = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = new OperationMessagePayload
                {
                    Query = "query"
                }
            };
            _transport.AddMessageToRead(subscribe);

            var unsubscribe = new OperationMessage
            {
                Type = MessageType.GQL_STOP,
                Id = "1"
            };
            _transport.AddMessageToRead(unsubscribe);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Empty(_sut.Subscriptions);
        }
    }
}
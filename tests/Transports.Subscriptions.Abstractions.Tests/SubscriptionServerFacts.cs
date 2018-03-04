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
            _documentExecuter = Substitute.For<IGraphQLExecuter>();
            _documentExecuter.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>
                    {
                        {"1", Substitute.For<IObservable<ExecutionResult>>()}
                    }
                });
            _subscriptionManager = new SubscriptionManager(_documentExecuter);
            _sut = new SubscriptionServer(_transport, _subscriptionManager);
        }

        private readonly TestableSubscriptionTransport _transport;
        private readonly SubscriptionServer _sut;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IGraphQLExecuter _documentExecuter;

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
        public async Task Receive_start_mutation()
        {
            /* Given */
            _documentExecuter.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new ExecutionResult());
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = new OperationMessagePayload
                {
                    Query = @"mutation AddMessage($message: MessageInputType!) {
  addMessage(message: $message) {
    from {
      id
      displayName
    }
    content
  }
}"
                }
            };
            _transport.AddMessageToRead(expected);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Empty(_sut.Subscriptions);
            Assert.Contains(_transport.WrittenMessages,
                message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transport.WrittenMessages,
                message => message.Type == MessageType.GQL_COMPLETE);
        }

        [Fact]
        public async Task Receive_start_query()
        {
            /* Given */
            _documentExecuter.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new ExecutionResult());
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = new OperationMessagePayload
                {
                    Query = @"{
  human() {
        name
        height
    }
}"
                }
            };
            _transport.AddMessageToRead(expected);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Empty(_sut.Subscriptions);
            Assert.Contains(_transport.WrittenMessages,
                message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transport.WrittenMessages,
                message => message.Type == MessageType.GQL_COMPLETE);
        }

        [Fact]
        public async Task Receive_start_subscription()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = new OperationMessagePayload
                {
                    Query = @"subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
  }
}"
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

        [Fact]
        public async Task Receive_terminate()
        {
            /* Given */
            var subscribe = new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_TERMINATE,
                Id = "1",
                Payload = new OperationMessagePayload
                {
                    Query = "query"
                }
            };
            _transport.AddMessageToRead(subscribe);

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Empty(_sut.Subscriptions);
        }

        [Fact]
        public async Task Receive_unknown()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = "x"
            };
            _transport.AddMessageToRead(expected);
            _transport.Complete();

            /* When */
            await _sut.ReceiveMessagesAsync();

            /* Then */
            Assert.Contains(_transport.WrittenMessages,
                message => message.Type == MessageType.GQL_CONNECTION_ERROR);
        }
    }
}
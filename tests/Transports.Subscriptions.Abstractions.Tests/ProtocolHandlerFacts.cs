using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class ProtocolHandlerFacts
    {
        private TestableSubscriptionTransport _transport;
        private TestableReader _transportReader;
        private TestableWriter _transportWriter;
        private IGraphQLExecuter _documentExecuter;
        private SubscriptionManager _subscriptionManager;
        private SubscriptionServer _server;
        private ProtocolMessageListener _sut;

        public ProtocolHandlerFacts()
        {
            _transport = new TestableSubscriptionTransport();
            _transportReader = _transport.Reader as TestableReader;
            _transportWriter = _transport.Writer as TestableWriter;
            _documentExecuter = Substitute.For<IGraphQLExecuter>();
            _documentExecuter.ExecuteAsync(null, null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>
                    {
                        {"1", Substitute.For<IObservable<ExecutionResult>>()}
                    }
                });
            _subscriptionManager = new SubscriptionManager(_documentExecuter, new NullLoggerFactory());
            _sut = new ProtocolMessageListener(new NullLogger<ProtocolMessageListener>());
            _server = new SubscriptionServer(
                _transport,
                _subscriptionManager,
                new[] { _sut },
                new NullLogger<SubscriptionServer>());
        }

        [Fact]
        public async Task Receive_init()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_INIT
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_CONNECTION_ACK);
        }

        [Fact]
        public async Task Receive_start_mutation()
        {
            /* Given */
            _documentExecuter.ExecuteAsync(null, null, null, null).ReturnsForAnyArgs(
                new ExecutionResult());
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = JObject.FromObject(new OperationMessagePayload
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
                })
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Empty(_server.Subscriptions);
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_COMPLETE);
        }

        [Fact]
        public async Task Receive_start_query()
        {
            /* Given */
            _documentExecuter.ExecuteAsync(null, null, null, null).ReturnsForAnyArgs(
                new ExecutionResult());
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    Query = @"{
  human() {
        name
        height
    }
}"
                })
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Empty(_server.Subscriptions);
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transportWriter.WrittenMessages,
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
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    Query = @"subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
  }
}"
                })
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Single(_server.Subscriptions, sub => sub.Id == expected.Id);
        }

        [Fact]
        public async Task Receive_stop()
        {
            /* Given */
            var subscribe = new OperationMessage
            {
                Type = MessageType.GQL_START,
                Id = "1",
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    Query = "query"
                })
            };
            _transportReader.AddMessageToRead(subscribe);

            var unsubscribe = new OperationMessage
            {
                Type = MessageType.GQL_STOP,
                Id = "1"
            };
            _transportReader.AddMessageToRead(unsubscribe);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Empty(_server.Subscriptions);
        }

        [Fact]
        public async Task Receive_terminate()
        {
            /* Given */
            var subscribe = new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_TERMINATE,
                Id = "1",
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    Query = "query"
                })
            };
            _transportReader.AddMessageToRead(subscribe);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Empty(_server.Subscriptions);
        }

        [Fact]
        public async Task Receive_unknown()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = "x"
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_CONNECTION_ERROR);
        }
    }
}
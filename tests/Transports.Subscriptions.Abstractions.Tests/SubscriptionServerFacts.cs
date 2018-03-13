using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class SubscriptionServerFacts
    {
        public SubscriptionServerFacts()
        {
            _messageListener = Substitute.For<IOperationMessageListener>();
            _transport = new TestableSubscriptionTransport();
            _transportReader = _transport.Reader as TestableReader;
            _transportWriter = _transport.Writer as TestableWriter;
            _documentExecuter = Substitute.For<IGraphQLExecuter>();
            _documentExecuter.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>
                    {
                        {"1", Substitute.For<IObservable<ExecutionResult>>()}
                    }
                });
            _subscriptionManager = new SubscriptionManager(_documentExecuter, new NullLoggerFactory());
            _sut = new SubscriptionServer(
                _transport,
                _subscriptionManager,
                new[] {_messageListener},
                new NullLogger<SubscriptionServer>());
        }

        private readonly TestableSubscriptionTransport _transport;
        private readonly SubscriptionServer _sut;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IGraphQLExecuter _documentExecuter;
        private readonly IOperationMessageListener _messageListener;
        private TestableReader _transportReader;
        private TestableWriter _transportWriter;

        [Fact]
        public async Task Listener_Handle()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_INIT
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _sut.OnConnect();

            /* Then */
            await _messageListener.Received().OnBeforeHandleAsync(_transportReader, _transportWriter, expected);
        }

        [Fact]
        public async Task Listener_Handled()
        {
            /* Given */
            var expected = new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_INIT
            };
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _sut.OnConnect();

            /* Then */
            await _messageListener.Received().OnAfterHandleAsync(_transportReader, _transportWriter, expected);
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
            await _sut.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages,
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
            await _sut.OnConnect();

            /* Then */
            Assert.Empty(_sut.Subscriptions);
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transportWriter.WrittenMessages,
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
            await _sut.OnConnect();

            /* Then */
            Assert.Empty(_sut.Subscriptions);
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
            await _sut.OnConnect();

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
            await _sut.OnConnect();

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
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    Query = "query"
                })
            };
            _transportReader.AddMessageToRead(subscribe);
            await _transportReader.Complete();

            /* When */
            await _sut.OnConnect();

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
            _transportReader.AddMessageToRead(expected);
            await _transportReader.Complete();

            /* When */
            await _sut.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages,
                message => message.Type == MessageType.GQL_CONNECTION_ERROR);
        }
    }
}
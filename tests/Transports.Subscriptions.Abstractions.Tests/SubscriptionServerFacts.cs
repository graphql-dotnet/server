using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
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
            _documentExecuter.ExecuteAsync(null, null, null, null).ReturnsForAnyArgs(
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
        private readonly TestableReader _transportReader;
        private readonly TestableWriter _transportWriter;


        [Fact]
        public async Task Listener_BeforeHandle()
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
            await _messageListener.Received().BeforeHandleAsync(Arg.Is<MessageHandlingContext>(context =>
                context.Writer == _transportWriter
                && context.Reader == _transportReader
                && context.Subscriptions == _subscriptionManager
                && context.Message == expected));
        }

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
            await _messageListener.Received().HandleAsync(Arg.Is<MessageHandlingContext>(context =>
                context.Writer == _transportWriter
                && context.Reader == _transportReader
                && context.Subscriptions == _subscriptionManager
                && context.Message == expected));
        }

        [Fact]
        public async Task Listener_AfterHandle()
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
            await _messageListener.Received().AfterHandleAsync(Arg.Is<MessageHandlingContext>(context =>
                context.Writer == _transportWriter
                && context.Reader == _transportReader
                && context.Subscriptions == _subscriptionManager
                && context.Message == expected));
        }
    }
}
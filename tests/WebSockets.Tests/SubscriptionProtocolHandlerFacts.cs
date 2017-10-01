using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Subscription;
using GraphQL.Transports.AspNetCore.Requests;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NSubstitute.Core;
using Xunit;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class SubscriptionProtocolHandlerFacts
    {
        public SubscriptionProtocolHandlerFacts()
        {
            _schema = new TestSchema();
            _documentExecuter = Substitute.For<IDocumentExecuter>();
            _subscriptionExecuter = Substitute.For<ISubscriptionExecuter>();
            _messageWriter = Substitute.For<IJsonMessageWriter>();

            var logger = Substitute.For<ILogger<SubscriptionProtocolHandler<TestSchema>>>();
            _sut = new SubscriptionProtocolHandler<TestSchema>(
                _schema,
                _subscriptionExecuter,
                _documentExecuter,
                logger);
        }

        private readonly TestSchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ISubscriptionExecuter _subscriptionExecuter;
        private readonly SubscriptionProtocolHandler<TestSchema> _sut;
        private readonly IJsonMessageWriter _messageWriter;

        private SubscriptionExecutionResult CreateStreamResult(CallInfo arg)
        {
            var streams = new ConcurrentDictionary<string, IObservable<ExecutionResult>>();
            streams.TryAdd("test", new Subject<ExecutionResult>());
            return new SubscriptionExecutionResult
            {
                Streams = streams
            };
        }

        private OperationMessageContext CreateMessage(string type, object payload)
        {
            var op = new OperationMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Payload = payload != null ? JObject.FromObject(payload) : null
            };

            return new OperationMessageContext("1", _messageWriter, op);
        }

        [Fact]
        public async Task should_handle_init()
        {
            /* Given */
            var messageContext = CreateMessage(
                MessageTypes.GQL_CONNECTION_INIT, null);

            /* When */
            await _sut.HandleMessageAsync(messageContext).ConfigureAwait(false);

            /* Then */
            await messageContext.MessageWriter
                .Received()
                .WriteMessageAsync(Arg.Is<OperationMessage>(
                    message => message.Type == MessageTypes.GQL_CONNECTION_ACK)).ConfigureAwait(false);
        }

        [Fact]
        public async Task should_handle_start()
        {
            /* Given */
            var query = new GraphQuery
            {
                OperationName = "test",
                Query = "query",
                Variables = JObject.FromObject(new {test = "variable"})
            };

            var messageContext = CreateMessage(
                MessageTypes.GQL_START, query);

            _subscriptionExecuter.SubscribeAsync(Arg.Any<ExecutionOptions>())
                .Returns(CreateStreamResult);

            /* When */
            await _sut.HandleMessageAsync(messageContext).ConfigureAwait(false);

            /* Then */
            await _subscriptionExecuter.Received()
                .SubscribeAsync(Arg.Is<ExecutionOptions>(
                    context => context.Schema == _schema
                               && context.Query == query.Query
                               && context.Inputs.ContainsKey("test")))
                .ConfigureAwait(false);
            var connectionSubscriptions = _sut.Subscriptions[messageContext.ConnectionId];
            Assert.True(connectionSubscriptions.ContainsKey(messageContext.Op.Id));
        }

        [Fact]
        public async Task should_handle_stop()
        {
            /* Given */
            var messageContext = CreateMessage(
                MessageTypes.GQL_STOP, null);

            await _sut.AddSubscription(messageContext, CreateStreamResult(null)).ConfigureAwait(false);

            /* When */
            await _sut.HandleMessageAsync(messageContext).ConfigureAwait(false);

            /* Then */
            await messageContext.MessageWriter
                .Received()
                .WriteMessageAsync(Arg.Is<OperationMessage>(
                    message => message.Type == MessageTypes.GQL_COMPLETE)).ConfigureAwait(false);

            var connectionSubscriptions = _sut.Subscriptions[messageContext.ConnectionId];
            Assert.False(connectionSubscriptions.ContainsKey(messageContext.Op.Id));
        }

        [Fact]
        public async Task should_handle_terminate()
        {
            /* Given */
            var messageContext = CreateMessage(
                MessageTypes.GQL_CONNECTION_TERMINATE, null);

            await _sut.AddSubscription(messageContext, CreateStreamResult(null)).ConfigureAwait(false);

            /* When */
            await _sut.HandleMessageAsync(messageContext).ConfigureAwait(false);

            /* Then */
            await messageContext.MessageWriter
                .Received()
                .WriteMessageAsync(Arg.Is<OperationMessage>(
                    message => message.Type == MessageTypes.GQL_COMPLETE)).ConfigureAwait(false);

            Assert.False(_sut.Subscriptions.ContainsKey(messageContext.ConnectionId));
            Assert.True(_sut.Subscriptions.IsEmpty);
        }
    }
}

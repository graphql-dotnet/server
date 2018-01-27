using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Subscription;
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
            _determinator = Substitute.For<ISubscriptionDeterminator>();

            _connection = Substitute.For<IConnectionContext>();
            _connection.Writer.Returns(_messageWriter);
            _connection.ConnectionId.Returns("1");

            var logger = Substitute.For<ILogger<SubscriptionProtocolHandler<TestSchema>>>();
            _sut = new SubscriptionProtocolHandler<TestSchema>(
                _schema,
                _subscriptionExecuter,
                _documentExecuter,
                _determinator,
                logger);
        }

        private readonly TestSchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ISubscriptionExecuter _subscriptionExecuter;
        private readonly SubscriptionProtocolHandler<TestSchema> _sut;
        private readonly IJsonMessageWriter _messageWriter;
        private IConnectionContext _connection;
        private readonly ISubscriptionDeterminator _determinator;

        private SubscriptionExecutionResult CreateStreamResult(CallInfo arg)
        {
            var streams = new ConcurrentDictionary<string, IObservable<ExecutionResult>>();
            streams.TryAdd("test", new Subject<ExecutionResult>());
            return new SubscriptionExecutionResult
            {
                Streams = streams
            };
        }

        private OperationMessageContext CreateMessage(string type, GraphQLQuery payload)
        {
            var op = new OperationMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Payload = payload
            };

            return new OperationMessageContext(_connection, op);
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
        public async Task should_handle_start_subscriptions()
        {
            /* Given */
            var query = new GraphQLQuery
            {
                OperationName = "test",
                Query = "subscription",
                Variables = JObject.FromObject(new {test = "variable"})
            };

            var messageContext = CreateMessage(
                MessageTypes.GQL_START, query);

            _subscriptionExecuter.SubscribeAsync(Arg.Any<ExecutionOptions>())
                .Returns(CreateStreamResult);

            _determinator.IsSubscription(Arg.Any<ExecutionOptions>()).Returns(true);

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
        public async Task should_handle_start_others()
        {
            /* Given */
            var query = new GraphQLQuery
            {
                OperationName = "test",
                Query = "query",
                Variables = JObject.FromObject(new { test = "variable" })
            };
            var messageContext = CreateMessage(MessageTypes.GQL_START, query);

            var result = new object();
            _documentExecuter.ExecuteAsync(Arg.Any<ExecutionOptions>()).Returns(new ExecutionResult{ Data = result});

            _determinator.IsSubscription(Arg.Any<ExecutionOptions>()).Returns(false);

            /* When */
            await _sut.HandleMessageAsync(messageContext).ConfigureAwait(false);

            /* Then */
            await _documentExecuter.Received()
                .ExecuteAsync(Arg.Is<ExecutionOptions>(
                    context => context.Schema == _schema
                               && context.Query == query.Query
                               && context.Inputs.ContainsKey("test")))
                .ConfigureAwait(false);

            await _messageWriter.Received().WriteMessageAsync(Arg.Is<OperationMessage>(
                context => context.Type == MessageTypes.GQL_DATA
            )).ConfigureAwait(false);

            await _messageWriter.Received().WriteMessageAsync(Arg.Is<OperationMessage>(
                context => context.Type == MessageTypes.GQL_COMPLETE
            )).ConfigureAwait(false);
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class SubscriptionManagerFacts
    {
        public SubscriptionManagerFacts()
        {
            _writer = Substitute.For<IWriterPipeline>();
            _executer = Substitute.For<IGraphQLExecuter>();
            _executer.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>
                    {
                        {"1", Substitute.For<IObservable<ExecutionResult>>()}
                    }
                });
            _sut = new SubscriptionManager(_executer, new NullLoggerFactory());
        }

        private readonly SubscriptionManager _sut;
        private readonly IGraphQLExecuter _executer;
        private readonly IWriterPipeline _writer;

        [Fact]
        public async Task Failed_Subscribe_does_not_add()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            _executer.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Errors = new ExecutionErrors
                    {
                        new ExecutionError("error")
                    }
                });

            /* When */
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* Then */
            Assert.Empty(_sut);
        }

        [Fact]
        public async Task Failed_Subscribe_with_null_stream()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            _executer.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>
                    {
                        {"1", null}
                    }
                });

            /* When */
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* Then */
            await _writer.Received().SendAsync(
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageType.GQL_ERROR));
        }

        [Fact]
        public async Task Failed_Subscribe_writes_error()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            _executer.ExecuteAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult
                {
                    Errors = new ExecutionErrors
                    {
                        new ExecutionError("error")
                    }
                });

            /* When */
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* Then */
            await _writer.Received().SendAsync(
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageType.GQL_ERROR));
        }

        [Fact]
        public async Task Subscribe_adds()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            /* When */
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* Then */
            Assert.Single(_sut, sub => sub.Id == id);
        }

        [Fact]
        public async Task Subscribe_executes()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            /* When */
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* Then */
            await _executer.Received().ExecuteAsync(
                Arg.Is(payload.OperationName),
                Arg.Is(payload.Query),
                Arg.Any<dynamic>());
        }

        [Fact]
        public async Task Unsubscribe_removes()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* When */
            await _sut.UnsubscribeAsync(id);

            /* Then */
            Assert.Empty(_sut);
        }

        [Fact]
        public async Task Unsubscribe_writes_complete()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            await _sut.SubscribeOrExecuteAsync(id, payload, _writer);

            /* When */
            await _sut.UnsubscribeAsync(id);

            /* Then */
            await _writer.Received().SendAsync(
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageType.GQL_COMPLETE));
        }
    }
}
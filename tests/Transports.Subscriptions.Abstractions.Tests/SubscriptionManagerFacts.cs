using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class SubscriptionManagerFacts
    {
        private SubscriptionManager _sut;
        private ISubscriptionExecuter _executer;
        private ITargetBlock<OperationMessage> _writer;

        public SubscriptionManagerFacts()
        {
            _writer = Substitute.For<ITargetBlock<OperationMessage>>();
            _executer = Substitute.For<ISubscriptionExecuter>();
            _executer.SubscribeAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult()
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                    {
                        {"1", Substitute.For<IObservable<ExecutionResult>>()}
                    }
                });
            _sut = new SubscriptionManager(_executer);
        }

        [Fact]
        public async Task Subscribe_adds()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            /* When */
            await _sut.SubscribeAsync(id, payload, _writer);

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
            await _sut.SubscribeAsync(id, payload, _writer);

            /* Then */
            _executer.Received().SubscribeAsync(
                Arg.Is<string>(payload.OperationName),
                Arg.Is<string>(payload.Query),
                Arg.Any<dynamic>());
        }

        [Fact]
        public async Task Failed_Subscribe_writes_error()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            _executer.SubscribeAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult()
                {
                    Errors = new ExecutionErrors()
                    {
                        new ExecutionError("error")
                    }
                });

            /* When */
            await _sut.SubscribeAsync(id, payload, _writer);

            /* Then */
            _writer.Received().OfferMessage(
                Arg.Any<DataflowMessageHeader>(),
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                    && message.Type == MessageTypeConstants.GQL_ERROR),
                Arg.Any<ISourceBlock<OperationMessage>>(),
                Arg.Any<bool>());
        }

        [Fact]
        public async Task Failed_Subscribe_with_null_stream()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            _executer.SubscribeAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult()
                {
                    Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                    {
                        {"1", null}
                    }
                });

            /* When */
            await _sut.SubscribeAsync(id, payload, _writer);

            /* Then */
            _writer.Received().OfferMessage(
                Arg.Any<DataflowMessageHeader>(),
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageTypeConstants.GQL_ERROR),
                Arg.Any<ISourceBlock<OperationMessage>>(),
                Arg.Any<bool>());
        }

        [Fact]
        public async Task Failed_Subscribe_does_not_add()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();

            _executer.SubscribeAsync(null, null, null).ReturnsForAnyArgs(
                new SubscriptionExecutionResult()
                {
                    Errors = new ExecutionErrors()
                    {
                        new ExecutionError("error")
                    }
                });

            /* When */
            await _sut.SubscribeAsync(id, payload, _writer);

            /* Then */
            Assert.Empty(_sut);
        }

        [Fact]
        public async Task Unsubscribe_removes()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            await _sut.SubscribeAsync(id, payload, _writer);

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
            await _sut.SubscribeAsync(id, payload, _writer);

            /* When */
            await _sut.UnsubscribeAsync(id);

            /* Then */
            _writer.Received().OfferMessage(
                Arg.Any<DataflowMessageHeader>(),
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageTypeConstants.GQL_COMPLETE),
                Arg.Any<ISourceBlock<OperationMessage>>(),
                Arg.Any<bool>());
        }
    }
}
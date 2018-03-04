using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;
using NSubstitute;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class SubscriptionFacts
    {
        public SubscriptionFacts()
        {
            _writer = Substitute.For<ITargetBlock<OperationMessage>>();
        }

        private readonly ITargetBlock<OperationMessage> _writer;

        [Fact]
        public async Task Write_Complete_on_ubsubscribe()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            var result = new SubscriptionExecutionResult();

            var sut = new Subscription(id, payload, result, _writer, null);

            /* When */
            await sut.UnsubscribeAsync();


            /* Then */
            _writer.Received().OfferMessage(
                Arg.Any<DataflowMessageHeader>(),
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageTypeConstants.GQL_COMPLETE),
                Arg.Any<ISourceBlock<OperationMessage>>(),
                Arg.Any<bool>());
        }

        [Fact]
        public void Subscribe_to_stream()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            var stream = Substitute.For<IObservable<ExecutionResult>>();
            var result = new SubscriptionExecutionResult()
            {
                Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                {
                    {"op", stream}
                }
            };

            /* When */
            var sut = new Subscription(id, payload, result, _writer, null);


            /* Then */
            stream.Received().Subscribe(Arg.Is<Subscription>(sub => sub.Id == id));
        }

        [Fact]
        public async Task Unsubscribe_from_stream()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            var unsubscribe = Substitute.For<IDisposable>();
            var stream = Substitute.For<IObservable<ExecutionResult>>();
            stream.Subscribe(null).ReturnsForAnyArgs(unsubscribe);
            var result = new SubscriptionExecutionResult()
            {
                Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                {
                    {"op", stream}
                }
            };
            var sut = new Subscription(id, payload, result, _writer, null);

            /* When */
            await sut.UnsubscribeAsync();


            /* Then */
            unsubscribe.Received().Dispose();
        }

        [Fact]
        public void On_data_from_stream()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            var stream = new ReplaySubject<ExecutionResult>(1);
            var result = new SubscriptionExecutionResult()
            {
                Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                {
                    {"op", stream}
                }
            };
            var expected = new ExecutionResult();
            var sut = new Subscription(id, payload, result, _writer, null);

            /* When */
            stream.OnNext(expected);


            /* Then */
            _writer.Received().OfferMessage(
                Arg.Any<DataflowMessageHeader>(),
                Arg.Is<OperationMessage>(
                    message => message.Id == id
                               && message.Type == MessageTypeConstants.GQL_DATA),
                Arg.Any<ISourceBlock<OperationMessage>>(),
                Arg.Any<bool>());

        }

        [Fact]
        public void On_stream_complete()
        {
            /* Given */
            var id = "1";
            var payload = new OperationMessagePayload();
            var stream = new ReplaySubject<ExecutionResult>(1);
            var result = new SubscriptionExecutionResult()
            {
                Streams = new Dictionary<string, IObservable<ExecutionResult>>()
                {
                    {"op", stream}
                }
            };

            var completed = Substitute.For<Action<Subscription>>();
            var sut = new Subscription(id, payload, result, _writer, completed);

            /* When */
            stream.OnCompleted();


            /* Then */
            Assert.False(stream.HasObservers);
            completed.Received().Invoke(sut);
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
using GraphQL.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests;

public class SubscriptionManagerFacts
{
    public SubscriptionManagerFacts()
    {
        _writer = Substitute.For<IWriterPipeline>();
        _executer = Substitute.For<IDocumentExecuter>();
        _executer.ExecuteAsync(null).ReturnsForAnyArgs(
            new ExecutionResult
            {
                Streams = new Dictionary<string, IObservable<ExecutionResult>>
                {
                    { "1", Substitute.For<IObservable<ExecutionResult>>() }
                }
            });
        _sut = new SubscriptionManager(_executer, new NullLoggerFactory(), NoopServiceScopeFactory.Instance);
        _server = new TestableServerOperations(null, _writer, _sut);
    }

    private readonly SubscriptionManager _sut;
    private readonly IDocumentExecuter _executer;
    private readonly IWriterPipeline _writer;
    private readonly IServerOperations _server;

    [Fact]
    public async Task Failed_Subscribe_does_not_add()
    {
        /* Given */
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        _executer.ExecuteAsync(null).ReturnsForAnyArgs(
            new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("error")
                }
            });

        /* When */
        await _sut.SubscribeOrExecuteAsync(id, payload, context);

        /* Then */
        Assert.Empty(_sut);
    }

    [Fact]
    public async Task Failed_Subscribe_with_null_stream()
    {
        /* Given */
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        _executer.ExecuteAsync(null).ReturnsForAnyArgs(
            new ExecutionResult
            {
                Streams = new Dictionary<string, IObservable<ExecutionResult>>
                {
                    { "1", null }
                }
            });

        /* When */
        await _sut.SubscribeOrExecuteAsync(id, payload, context);

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
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        _executer.ExecuteAsync(null).ReturnsForAnyArgs(
            new ExecutionResult
            {
                Errors = new ExecutionErrors
                {
                    new ExecutionError("error")
                }
            });

        /* When */
        await _sut.SubscribeOrExecuteAsync(id, payload, context);

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
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        /* When */
        await _sut.SubscribeOrExecuteAsync(id, payload, context);

        /* Then */
        Assert.Single(_sut, sub => sub.Id == id);
    }

    [Fact]
    public async Task Subscribe_executes()
    {
        /* Given */
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        /* When */
        await _sut.SubscribeOrExecuteAsync(id, payload, context);

        /* Then */
        await _executer.Received().ExecuteAsync(
            Arg.Any<ExecutionOptions>());
    }

    [Fact]
    public async Task Unsubscribe_removes()
    {
        /* Given */
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        await _sut.SubscribeOrExecuteAsync(id, payload, context);

        /* When */
        await _sut.UnsubscribeAsync(id);

        /* Then */
        Assert.Empty(_sut);
    }

    [Fact]
    public async Task Unsubscribe_writes_complete()
    {
        /* Given */
        string id = "1";
        var payload = new GraphQLRequest();
        var context = new MessageHandlingContext(_server, null);

        await _sut.SubscribeOrExecuteAsync(id, payload, context);

        /* When */
        await _sut.UnsubscribeAsync(id);

        /* Then */
        await _writer.Received().SendAsync(
            Arg.Is<OperationMessage>(
                message => message.Id == id
                           && message.Type == MessageType.GQL_COMPLETE));
    }
}

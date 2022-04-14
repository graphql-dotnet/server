namespace GraphQL.Server.Transports.Subscriptions.Abstractions;

public interface IServerOperations //todo: inherit IDisposable
{
    Task Terminate();

    IReaderPipeline TransportReader { get; }

    IWriterPipeline TransportWriter { get; }

    ISubscriptionManager Subscriptions { get; }
}

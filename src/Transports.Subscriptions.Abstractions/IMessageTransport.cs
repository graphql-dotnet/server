namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IMessageTransport
    {
        IReaderPipeline Reader { get; }

        IWriterPipeline Writer { get; }
    }
}
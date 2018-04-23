using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IServerOperations
    {
        Task Terminate();

        IReaderPipeline TransportReader { get; }

        IWriterPipeline TransportWriter { get; }

        ISubscriptionManager Subscriptions { get; }
    }
}
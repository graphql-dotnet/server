using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class TestableServerOperations : IServerOperations
    {
        public TestableServerOperations(
            IReaderPipeline reader,
            IWriterPipeline writer,
            ISubscriptionManager subscriptions)
        {
            TransportReader = reader;
            TransportWriter = writer;
            Subscriptions = subscriptions;
        }

        public Task Terminate()
        {
            IsTerminated = true;
            return Task.CompletedTask;
        }

        public bool IsTerminated { get; set; }

        public IReaderPipeline TransportReader { get; }
        public IWriterPipeline TransportWriter { get; }
        public ISubscriptionManager Subscriptions { get; }
    }
}
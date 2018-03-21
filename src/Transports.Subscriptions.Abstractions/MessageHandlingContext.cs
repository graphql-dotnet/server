using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class MessageHandlingContext
    {
        private readonly SubscriptionServer _server;

        public IReaderPipeline Reader { get; }

        public IWriterPipeline Writer { get; }

        public ISubscriptionManager Subscriptions { get; }

        public OperationMessage Message { get; }


        public MessageHandlingContext(
            SubscriptionServer server, 
            OperationMessage message)
        {
            _server = server;
            Reader = server.TransportReader;
            Writer = server.TransportWriter;
            Subscriptions = server.Subscriptions;
            Message = message;
        }

        public Task Terminate()
        {
            return _server.Terminate();
        }
    }
}
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class SubscriptionServer
    {
        private readonly IMessageTransport _transport;
        private readonly Task _completion;

        public SubscriptionServer(IMessageTransport transport)
        {
            _transport = transport;
            _completion = LinkToTransportReader();
        }

        private Task LinkToTransportReader()
        {
            var handler = new ActionBlock<OperationMessage>(HandleMessageAsync, new ExecutionDataflowBlockOptions()
            {
                EnsureOrdered = true
            });

            var reader = new BufferBlock<OperationMessage>(new DataflowBlockOptions()
            {
                EnsureOrdered = true
            });

            reader.LinkTo(handler, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            _transport.Reader.LinkTo(reader, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            return handler.Completion;
        }

        private Task HandleMessageAsync(OperationMessage message)
        {
            switch (message.Type)
            {
                case MessageTypeConstants.GQL_CONNECTION_INIT:
                    return HandleInitAsync();
            }

            return Task.CompletedTask;
        }

        private Task HandleInitAsync()
        {
            return _transport.Writer.SendAsync(new OperationMessage()
            {
                Type = MessageTypeConstants.GQL_CONNECTION_ACK
            });
        }

        public Task ReceiveMessagesAsync()
        {
            return _completion;
        }
    }
}
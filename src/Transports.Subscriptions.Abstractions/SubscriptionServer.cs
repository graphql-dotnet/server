using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class SubscriptionServer
    {
        private readonly Task _completion;
        private readonly IMessageTransport _transport;

        public SubscriptionServer(IMessageTransport transport, ISubscriptionManager subscriptions)
        {
            Subscriptions = subscriptions;
            _transport = transport;
            _completion = LinkToTransportReader();
        }

        public ISubscriptionManager Subscriptions { get; }

        private Task LinkToTransportReader()
        {
            var handler = new ActionBlock<OperationMessage>(HandleMessageAsync, new ExecutionDataflowBlockOptions
            {
                EnsureOrdered = true
            });

            var reader = new BufferBlock<OperationMessage>(new DataflowBlockOptions
            {
                EnsureOrdered = true
            });

            reader.LinkTo(handler, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            _transport.Reader.LinkTo(reader, new DataflowLinkOptions
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
                    return HandleInitAsync(message);
                case MessageTypeConstants.GQL_START:
                    return HandleStartAsync(message);
                case MessageTypeConstants.GQL_STOP:
                    return HandleStopAsync(message);
                default:
                    throw new InvalidOperationException($"Unknown message type {message.Type}");
            }
        }

        private Task HandleStopAsync(OperationMessage message)
        {
            return Subscriptions.UnsubscribeAsync(message.Id);
        }

        private Task HandleStartAsync(OperationMessage message)
        {
            if (!(message.Payload is OperationMessagePayload payload))
            {
                throw new InvalidOperationException($"Could not get OperationMessagePayload from message.Payload");
            }

            return Subscriptions.SubscribeAsync(
                message.Id,
                payload,
                _transport.Writer);
        }

        private Task HandleInitAsync(OperationMessage message)
        {
            return _transport.Writer.SendAsync(new OperationMessage
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
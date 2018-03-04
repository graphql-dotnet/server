using System;
using System.Collections.Generic;
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
                case MessageType.GQL_CONNECTION_INIT:
                    return HandleInitAsync(message);
                case MessageType.GQL_START:
                    return HandleStartAsync(message);
                case MessageType.GQL_STOP:
                    return HandleStopAsync(message);
                case MessageType.GQL_CONNECTION_TERMINATE:
                    return HandleTerminateAsync(message);
                default:
                    return WriteErrorAsync(message);
            }
        }

        private Task WriteErrorAsync(OperationMessage message)
        {
            return _transport.Writer.SendAsync(new OperationMessage()
            {
                Type = MessageType.GQL_CONNECTION_ERROR,
                Id = message.Id,
                Payload = new
                {
                    message.Id,
                    Errors = new ExecutionErrors()
                    {
                        new ExecutionError($"Unexpected message type {message.Type}")
                    }
                }
            });
        }

        private async Task HandleTerminateAsync(OperationMessage message)
        {
            foreach (var subscription in Subscriptions)
                await Subscriptions.UnsubscribeAsync(subscription.Id);

            _transport.Writer.Complete();
            _transport.Reader.Complete();
        }

        private Task HandleStopAsync(OperationMessage message)
        {
            return Subscriptions.UnsubscribeAsync(message.Id);
        }

        private Task HandleStartAsync(OperationMessage message)
        {
            if (!(message.Payload is OperationMessagePayload payload))
                throw new InvalidOperationException($"Could not get OperationMessagePayload from message.Payload");

            return Subscriptions.SubscribeAsync(
                message.Id,
                payload,
                _transport.Writer);
        }

        private Task HandleInitAsync(OperationMessage message)
        {
            return _transport.Writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ACK
            });
        }

        public Task ReceiveMessagesAsync()
        {
            return _completion;
        }
    }
}
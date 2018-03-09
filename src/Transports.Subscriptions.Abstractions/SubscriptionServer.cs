using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class SubscriptionServer
    {
        private readonly ILogger<SubscriptionServer> _logger;
        private readonly Task _completion;

        public SubscriptionServer(
            IMessageTransport transport, 
            ISubscriptionManager subscriptions, 
            ILogger<SubscriptionServer> logger)
        {
            _logger = logger;
            Subscriptions = subscriptions;
            Transport = transport;
            _completion = LinkToTransportReader();
        }

        public IMessageTransport Transport { get; }

        public ISubscriptionManager Subscriptions { get; }

        private Task LinkToTransportReader()
        {
            _logger.LogDebug("Creating reader pipeline");
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

            Transport.Reader.LinkTo(reader, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            _logger.LogDebug("Reader pipeline created");
            return handler.Completion;
        }

        private Task HandleMessageAsync(OperationMessage message)
        {
            _logger.LogDebug("Handling message: {id} of type: {type}", message.Id, message.Type);
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
                    return HandleUnknownAsync(message);
            }
        }

        private Task HandleUnknownAsync(OperationMessage message)
        {
            _logger.LogError($"Unexpected message type: {message.Type}");
            return Transport.Writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ERROR,
                Id = message.Id,
                Payload = JObject.FromObject(new
                {
                    message.Id,
                    Errors = new ExecutionErrors
                    {
                        new ExecutionError($"Unexpected message type {message.Type}")
                    }
                })
            });
        }

        private async Task HandleTerminateAsync(OperationMessage message)
        {
            _logger.LogInformation("Handle terminate");
            foreach (var subscription in Subscriptions)
                await Subscriptions.UnsubscribeAsync(subscription.Id);

            Transport.Complete();
        }

        private Task HandleStopAsync(OperationMessage message)
        {
            _logger.LogInformation("Handle stop: {id}", message.Id);
            return Subscriptions.UnsubscribeAsync(message.Id);
        }

        private Task HandleStartAsync(OperationMessage message)
        {
            _logger.LogInformation("Handle start: {id}", message.Id);
            var payload = message.Payload.ToObject<OperationMessagePayload>();
            if (payload == null)
                throw new InvalidOperationException($"Could not get OperationMessagePayload from message.Payload");

            return Subscriptions.SubscribeAsync(
                message.Id,
                payload,
                Transport.Writer);
        }

        private Task HandleInitAsync(OperationMessage message)
        {
            _logger.LogInformation("Handle init");
            return Transport.Writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ACK
            });
        }

        public Task OnConnected()
        {
            _logger.LogInformation("Serving...");
            return Task.WhenAll(Transport.Completion, _completion).ContinueWith(
                result =>
                {
                    if (result.Exception != null)
                    {
                        _logger.LogError("Server closed with error: {error}", result.Exception);
                    }
                    _logger.LogInformation("Server stopped");
                });
        }
    }
}
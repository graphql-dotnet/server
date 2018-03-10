using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class SubscriptionServer
    {
        private readonly IEnumerable<IOperationMessageListener> _operationMessageListeners;
        private readonly ILogger<SubscriptionServer> _logger;
        private ActionBlock<OperationMessage> _handler;
        private ITargetBlock<OperationMessage> _writer;

        public SubscriptionServer(
            IMessageTransport transport, 
            ISubscriptionManager subscriptions, 
            IEnumerable<IOperationMessageListener> operationMessageListeners,
            ILogger<SubscriptionServer> logger)
        {
            _operationMessageListeners = operationMessageListeners;
            _logger = logger;
            Subscriptions = subscriptions;
            Transport = transport;
        }

        public IMessageTransport Transport { get; }

        public ISubscriptionManager Subscriptions { get; }
        
        private void LinkToTransportReader()
        {
            _logger.LogDebug("Creating reader pipeline");
            var transportReader = Transport.CreateReader();
            var handler = new ActionBlock<OperationMessage>(HandleMessageAsync, new ExecutionDataflowBlockOptions
            {
                EnsureOrdered = true
            });

            transportReader.LinkTo(handler, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });

            _handler = handler;
            _logger.LogDebug("Reader pipeline created");
        }

        private async Task HandleMessageAsync(OperationMessage message)
        {
            _logger.LogDebug("Handling message: {id} of type: {type}", message.Id, message.Type);
            await OnHandleMessageAsync(message);

            switch (message.Type)
            {
                case MessageType.GQL_CONNECTION_INIT:
                    await HandleInitAsync(message);
                    break;
                case MessageType.GQL_START:
                    await HandleStartAsync(message);
                    break;
                case MessageType.GQL_STOP:
                    await HandleStopAsync(message);
                    break;
                case MessageType.GQL_CONNECTION_TERMINATE:
                    await HandleTerminateAsync(message);
                    break;
                default:
                    await HandleUnknownAsync(message);
                    break;
            }

            await OnMessageHandledAsync(message);
        }

        private async Task OnHandleMessageAsync(OperationMessage message)
        {
            foreach (var listener in _operationMessageListeners)
                await listener.OnHandleMessageAsync(Transport, message);
        }

        private async Task OnMessageHandledAsync(OperationMessage message)
        {
            foreach (var listener in _operationMessageListeners)
                await listener.OnMessageHandledAsync(Transport, message);
        }

        private Task HandleUnknownAsync(OperationMessage message)
        {
            _logger.LogError($"Unexpected message type: {message.Type}");
            return _writer.SendAsync(new OperationMessage
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
            await Terminate();
        }

        private async Task Terminate()
        {
            foreach (var subscription in Subscriptions)
                await Subscriptions.UnsubscribeAsync(subscription.Id);

            await Transport.CloseAsync();
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
                _writer);
        }

        private Task HandleInitAsync(OperationMessage message)
        {
            _logger.LogInformation("Handle init");
            return _writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ACK
            });
        }

        public Task OnConnect()
        {
            _logger.LogInformation("Serving...");
            LinkToTransportReader();
            LinkToTransportWriter();

            return Task.WhenAll(_handler.Completion).ContinueWith(
                result =>
                {
                    if (result.Exception != null)
                    {
                        _logger.LogError("Server closed with error: {error}", result.Exception);
                    }
                    _logger.LogInformation("Server stopped");
                });
        }

        private void LinkToTransportWriter()
        {
            _logger.LogDebug("Creating writer pipeline");
            _writer = Transport.CreateWriter();
            _logger.LogDebug("Writer pipeline created");
        }

        public Task OnDisconnect()
        {
            return Terminate();
        }
    }
}
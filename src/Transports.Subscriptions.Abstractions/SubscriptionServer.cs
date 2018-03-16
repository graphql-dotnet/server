using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Subscription server
    ///     Acts as a message pump reading, handling and writing messages
    /// </summary>
    public class SubscriptionServer
    {
        private readonly ILogger<SubscriptionServer> _logger;
        private readonly IEnumerable<IOperationMessageListener> _operationMessageListeners;
        private ActionBlock<OperationMessage> _handler;

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

        public IReaderPipeline TransportReader { get; set; }

        public IWriterPipeline TransportWriter { get; set; }

        public async Task OnConnect()
        {
            _logger.LogInformation("Connected...");
            LinkToTransportWriter();
            LinkToTransportReader();

            await _handler.Completion;
            await TransportWriter.Complete();
            await TransportWriter.Completion;
        }

        public Task OnDisconnect()
        {
            return Terminate();
        }

        private void LinkToTransportReader()
        {
            _logger.LogDebug("Creating reader pipeline");
            TransportReader = Transport.Reader;
            _handler = new ActionBlock<OperationMessage>(HandleMessageAsync, new ExecutionDataflowBlockOptions
            {
                EnsureOrdered = true,
                BoundedCapacity = 1
            });

            TransportReader.LinkTo(_handler);
            _logger.LogDebug("Reader pipeline created");
        }

        private async Task HandleMessageAsync(OperationMessage message)
        {
            _logger.LogDebug("Handling message: {id} of type: {type}", message.Id, message.Type);
            await OnBeforeHandleAsync(message);

            //todo(pekka): should this be changed into message listener?
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

            await OnAfterHandleAsync(message);
        }

        private async Task OnBeforeHandleAsync(OperationMessage message)
        {
            foreach (var listener in _operationMessageListeners)
                await listener.OnBeforeHandleAsync(TransportReader, TransportWriter, message);
        }

        private async Task OnAfterHandleAsync(OperationMessage message)
        {
            foreach (var listener in _operationMessageListeners)
                await listener.OnAfterHandleAsync(TransportReader, TransportWriter, message);
        }

        private Task HandleUnknownAsync(OperationMessage message)
        {
            _logger.LogError($"Unexpected message type: {message.Type}");
            return TransportWriter.SendAsync(new OperationMessage
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

            await TransportReader.Complete();
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

            return Subscriptions.SubscribeOrExecuteAsync(
                message.Id,
                payload,
                TransportWriter);
        }

        private Task HandleInitAsync(OperationMessage message)
        {
            _logger.LogInformation("Handle init");
            return TransportWriter.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ACK
            });
        }

        private void LinkToTransportWriter()
        {
            _logger.LogDebug("Creating writer pipeline");
            TransportWriter = Transport.Writer;
            _logger.LogDebug("Writer pipeline created");
        }
    }
}
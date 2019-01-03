using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class ProtocolMessageListener : IOperationMessageListener
    {
        private readonly ILogger<ProtocolMessageListener> _logger;

        public ProtocolMessageListener(ILogger<ProtocolMessageListener> logger)
        {
            _logger = logger;
        }


        public Task BeforeHandleAsync(MessageHandlingContext context)
        {
            return Task.CompletedTask;
        }

        public Task HandleAsync(MessageHandlingContext context)
        {
            if (context.Terminated)
                return Task.CompletedTask;

            var message = context.Message;
            switch (message.Type)
            {
                case MessageType.GQL_CONNECTION_INIT:
                    return HandleInitAsync(context);
                case MessageType.GQL_START:
                    return HandleStartAsync(context);
                case MessageType.GQL_STOP:
                    return HandleStopAsync(context);
                case MessageType.GQL_CONNECTION_TERMINATE:
                    return HandleTerminateAsync(context);
                default:
                    return HandleUnknownAsync(context);
            }
        }

        public Task AfterHandleAsync(MessageHandlingContext context)
        {
            return Task.CompletedTask;
        }

        private Task HandleUnknownAsync(MessageHandlingContext context)
        {
            var message = context.Message;
            _logger.LogError($"Unexpected message type: {message.Type}");
            return context.Writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ERROR,
                Id = message.Id,
                Payload = new ExecutionResult
                {
                    Errors = new ExecutionErrors
                    {
                        new ExecutionError($"Unexpected message type {message.Type}")
                    }
                }
            });
        }

        private Task HandleStopAsync(MessageHandlingContext context)
        {
            var message = context.Message;
            _logger.LogDebug("Handle stop: {id}", message.Id);
            return context.Subscriptions.UnsubscribeAsync(message.Id);
        }

        private Task HandleStartAsync(MessageHandlingContext context)
        {
            var message = context.Message;
            _logger.LogDebug("Handle start: {id}", message.Id);
            var payload = ((JObject)message.Payload).ToObject<OperationMessagePayload>();
            if (payload == null)
                throw new InvalidOperationException($"Could not get OperationMessagePayload from message.Payload");

            return context.Subscriptions.SubscribeOrExecuteAsync(
                message.Id,
                payload,
                context);
        }

        private Task HandleInitAsync(MessageHandlingContext context)
        {
            _logger.LogDebug("Handle init");
            return context.Writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ACK
            });
        }

        private Task HandleTerminateAsync(MessageHandlingContext context)
        {
            _logger.LogDebug("Handle terminate");
            return context.Terminate();
        }
    }
}
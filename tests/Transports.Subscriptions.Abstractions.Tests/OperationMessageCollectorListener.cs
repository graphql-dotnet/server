using System.Collections.Concurrent;
using GraphQL.Transport;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class OperationMessageCollectorListener : IOperationMessageListener
    {
        public ConcurrentBag<OperationMessage> HandleMessages { get; } = new ConcurrentBag<OperationMessage>();

        public ConcurrentBag<OperationMessage> HandledMessages { get; } = new ConcurrentBag<OperationMessage>();

        public Task BeforeHandleAsync(MessageHandlingContext context) => Task.CompletedTask;

        public Task HandleAsync(MessageHandlingContext context)
        {
            HandleMessages.Add(context.Message);
            return Task.CompletedTask;
        }

        public Task AfterHandleAsync(MessageHandlingContext context)
        {
            HandledMessages.Add(context.Message);
            return Task.CompletedTask;
        }
    }
}

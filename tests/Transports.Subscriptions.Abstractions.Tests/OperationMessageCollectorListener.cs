using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class OperationMessageCollectorListener : IOperationMessageListener
    {
        public ConcurrentBag<OperationMessage> HandleMessages { get; } = new ConcurrentBag<OperationMessage>();

        public ConcurrentBag<OperationMessage> HandledMessages { get; } = new ConcurrentBag<OperationMessage>();

        public Task OnHandleMessageAsync(IMessageTransport transport, OperationMessage message)
        {
            HandleMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task OnMessageHandledAsync(IMessageTransport transport, OperationMessage message)
        {
            HandledMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
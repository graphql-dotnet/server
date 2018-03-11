using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class OperationMessageCollectorListener : IOperationMessageListener
    {
        public ConcurrentBag<OperationMessage> HandleMessages { get; } = new ConcurrentBag<OperationMessage>();

        public ConcurrentBag<OperationMessage> HandledMessages { get; } = new ConcurrentBag<OperationMessage>();

        public Task OnHandleMessageAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message)
        {
            HandleMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task OnMessageHandledAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message)
        {
            HandledMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class LogMessagesListener : IOperationMessageListener
    {
        private readonly ILogger<LogMessagesListener> _logger;

        public LogMessagesListener(ILogger<LogMessagesListener> logger)
        {
            _logger = logger;
        }

        public Task OnBeforeHandleAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message)
        {
            _logger.LogDebug("Received message: {message}", message);
            return Task.CompletedTask;
        }

        public Task OnAfterHandleAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message)
        {
            return Task.CompletedTask;
        }
    }
}
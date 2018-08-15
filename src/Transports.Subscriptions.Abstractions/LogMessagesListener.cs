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

        public Task BeforeHandleAsync(MessageHandlingContext context)
        {
            return Task.FromResult(true);
        }

        public Task HandleAsync(MessageHandlingContext context)
        {
            _logger.LogDebug("Received message: {message}", context.Message);
            return Task.CompletedTask;
        }

        public Task AfterHandleAsync(MessageHandlingContext context)
        {
            return Task.CompletedTask;
        }
    }
}
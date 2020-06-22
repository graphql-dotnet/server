using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class LogMessagesListener : IOperationMessageListener
    {
        private readonly ILogger<LogMessagesListener> _logger;

        public LogMessagesListener(ILogger<LogMessagesListener> logger)
        {
            _logger = logger;
        }

        public Task BeforeHandleAsync(MessageHandlingContext context) => Task.FromResult(true);

        public Task HandleAsync(MessageHandlingContext context)
        {
            _logger.LogDebug("Received message: {message}", context.Message);
            return Task.CompletedTask;
        }

        public Task AfterHandleAsync(MessageHandlingContext context) => Task.CompletedTask;
    }
}

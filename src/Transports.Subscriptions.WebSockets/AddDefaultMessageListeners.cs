using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public class AddDefaultMessageListeners<TSchema> : IConfigureOptions<ExecutionOptions<TSchema>> where TSchema : ISchema
    {
        private readonly ILoggerFactory _loggerFactory;

        public AddDefaultMessageListeners(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Configure(ExecutionOptions<TSchema> options)
        {
            options.MessageListeners.Insert(0, new LogMessagesListener(_loggerFactory.CreateLogger<LogMessagesListener>()));
            options.MessageListeners.Add(new ProtocolMessageListener(_loggerFactory.CreateLogger<ProtocolMessageListener>()));
        }
    }
}
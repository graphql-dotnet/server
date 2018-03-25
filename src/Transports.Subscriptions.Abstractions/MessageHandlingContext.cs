using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GraphQL.Validation;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class MessageHandlingContext : IDisposable
    {
        private readonly SubscriptionServer _server;


        public MessageHandlingContext(
            SubscriptionServer server,
            OperationMessage message)
        {
            _server = server;
            Reader = server.TransportReader;
            Writer = server.TransportWriter;
            Subscriptions = server.Subscriptions;
            Message = message;
        }

        public IReaderPipeline Reader { get; }

        public IWriterPipeline Writer { get; }

        public ISubscriptionManager Subscriptions { get; }

        public OperationMessage Message { get; }

        public ConcurrentDictionary<string, object> Properties { get; protected set; } =
            new ConcurrentDictionary<string, object>();

        public T Get<T>(string key)
        {
            if (!Properties.TryGetValue(key, out var value)) return default(T);

            if (value is T variable)
                return variable;

            return default(T);
        }

        public void Dispose()
        {
            foreach (var property in Properties)
            {
                if (property.Value is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public Task Terminate()
        {
            return _server.Terminate();
        }
    }
}
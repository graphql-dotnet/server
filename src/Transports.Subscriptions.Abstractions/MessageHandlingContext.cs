using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class MessageHandlingContext : IDisposable
    {
        private readonly IServerOperations _server;

        public MessageHandlingContext(
            IServerOperations server,
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

        public bool Terminated { get; set; }

        public void Dispose()
        {
            foreach (var property in Properties)
                if (property.Value is IDisposable disposable)
                    disposable.Dispose();
        }

        public T Get<T>(string key)
        {
            if (!Properties.TryGetValue(key, out var value)) return default(T);

            if (value is T variable)
                return variable;

            return default(T);
        }

        public Task Terminate()
        {
            Terminated = true;
            return _server.Terminate();
        }
    }
}
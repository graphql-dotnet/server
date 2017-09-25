using System;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionHandle : IObserver<object>, IDisposable
    {
        private readonly IJsonMessageWriter _messageWriter;
        private readonly IDocumentWriter _documentWriter;

        public SubscriptionHandle(OperationMessage op,
            IObservable<object> stream,
            IJsonMessageWriter messageWriter,
            IDocumentWriter documentWriter)
        {
            Op = op;
            Stream = stream;
            _messageWriter = messageWriter;
            _documentWriter = documentWriter;
            Unsubscribe = stream.Subscribe(this);
        }

        public OperationMessage Op { get; }

        public IObservable<object> Stream { get; }

        public IDisposable Unsubscribe { get; set; }

        public void Dispose()
        {
            Unsubscribe?.Dispose();

            // complete
            _messageWriter.WriteMessageAsync(new OperationMessage
            {
                Id = Op.Id,
                Type = MessageTypes.GQL_COMPLETE
            }).GetAwaiter().GetResult();
        }

        public void OnCompleted()
        {
            Unsubscribe.Dispose();
        }

        public void OnError(Exception error)
        {
            Unsubscribe.Dispose();
        }

        public void OnNext(object value)
        {
            var json = _documentWriter.Write(value);
            _messageWriter.WriteMessageAsync(new OperationMessage
            {
                Id = Op.Id,
                Type = MessageTypes.GQL_DATA,
                Payload = JObject.Parse(json)
            }).GetAwaiter().GetResult();
        }
    }
}

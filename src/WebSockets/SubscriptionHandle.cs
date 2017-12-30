using System;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Extensions;
using GraphQL.Server.Transports.WebSockets.Messages;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionHandle
    {
        private readonly IDocumentWriter _documentWriter;
        private readonly IJsonMessageWriter _messageWriter;

        public SubscriptionHandle(OperationMessage op,
            IObservable<object> stream,
            IJsonMessageWriter messageWriter,
            IDocumentWriter documentWriter)
        {
            Op = op;
            Stream = stream;
            _messageWriter = messageWriter;
            _documentWriter = documentWriter;
            Unsubscribe = stream.SubscribeAsync(OnNext, OnError, OnCompleted);
        }

        public OperationMessage Op { get; }

        public IObservable<object> Stream { get; }

        public IDisposable Unsubscribe { get; set; }

        public Task CloseAsync()
        {
            Unsubscribe?.Dispose();
            return _messageWriter.WriteMessageAsync(new OperationMessage
            {
                Id = Op.Id,
                Type = MessageTypes.GQL_COMPLETE
            });
        }

        public void OnCompleted()
        {
            Unsubscribe.Dispose();
        }

        public void OnError(Exception error)
        {
            Unsubscribe.Dispose();
        }

        public Task OnNext(object value)
        {
            var json = _documentWriter.Write(value);
            return _messageWriter.WriteMessageAsync(new OperationMessage
            {
                Id = Op.Id,
                Type = MessageTypes.GQL_DATA,
                Payload = JObject.Parse(json)
            });
        }
    }
}

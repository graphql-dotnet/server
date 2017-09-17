using System;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Messages;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionHandle : IObserver<object>, IDisposable
    {
        private readonly GraphQLConnectionContext _connection;
        private readonly IDocumentWriter _documentWriter;

        public SubscriptionHandle(OperationMessage op,
            IObservable<object> stream,
            GraphQLConnectionContext connection,
            IDocumentWriter documentWriter)
        {
            _connection = connection;
            Op = op;
            Stream = stream;
            _documentWriter = documentWriter;
            Unsubscribe = stream.Subscribe(this);
        }

        public OperationMessage Op { get; }

        public IObservable<object> Stream { get; }

        public IDisposable Unsubscribe { get; set; }

        public void Dispose()
        {
            Unsubscribe?.Dispose();
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
            _connection.Writer.WriteMessageAsync(new OperationMessage
            {
                Id = Op.Id,
                Type = MessageTypes.GQL_DATA,
                Payload = JObject.Parse(json)
            }).GetAwaiter().GetResult();
        }
    }
}

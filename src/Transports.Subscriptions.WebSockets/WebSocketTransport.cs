using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketTransport : IMessageTransport
    {
        private readonly WebSocket _socket;

        public WebSocketTransport(WebSocket socket, IDocumentWriter documentWriter)
        {
            _socket = socket;
            var serializerSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            Reader = new WebSocketReaderPipeline(_socket, serializerSettings);
            Writer = new WebSocketWriterPipeline(_socket, documentWriter);
        }

        public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

        public IReaderPipeline Reader { get; }
        public IWriterPipeline Writer { get; }

        public Task CloseAsync()
        {
            if (_socket.State != WebSocketState.Open)
                return Task.CompletedTask;

            if (CloseStatus.HasValue)
                if (CloseStatus != WebSocketCloseStatus.NormalClosure || CloseStatus != WebSocketCloseStatus.Empty)
                    return AbortAsync();

            return _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }

        private Task AbortAsync()
        {
            _socket.Abort();
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketTransport : IMessageTransport, IDisposable
    {
        private readonly WebSocket _socket;

        public WebSocketTransport(WebSocket socket, IGraphQLTextSerializer serializer)
        {
            _socket = socket;

            Reader = new WebSocketReaderPipeline(_socket, serializer);
            Writer = new WebSocketWriterPipeline(_socket, serializer);
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

        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}

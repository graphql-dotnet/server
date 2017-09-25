using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;

namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class WebSocketClient : IWebSocketClient
    {
        private readonly WebSocket _socket;

        public WebSocketClient(WebSocket socket)
        {
            _socket = socket;
        }

        public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

        public Task WriteMessageAsync(string message)
        {
            //todo: should throw or not?
            if (_socket.CloseStatus.HasValue)
                return Task.CompletedTask;

            var messageSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            return _socket.SendAsync(messageSegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task<string> ReadMessageAsync()
        {
            string message;
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);


            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    WebSocketReceiveResult receiveResult;

                    do
                    {
                        receiveResult = await _socket.ReceiveAsync(segment, CancellationToken.None);

                        if (receiveResult.CloseStatus.HasValue)
                            return null;

                        if (receiveResult.Count == 0)
                            continue;

                        await memoryStream.WriteAsync(segment.Array, segment.Offset, receiveResult.Count);
                    } while (!receiveResult.EndOfMessage || memoryStream.Length == 0);

                    message = Encoding.UTF8.GetString(memoryStream.ToArray());
                }
                catch
                {
                    return null;
                }
            }
            return message;
        }

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

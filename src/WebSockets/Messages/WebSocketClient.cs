using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Extensions;

namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class WebSocketClient : IWebSocketClient
    {
        private readonly WebSocket _socket;

        private readonly ConcurrentQueue<ArraySegment<byte>> _writeQueue = new ConcurrentQueue<ArraySegment<byte>>();
        private IDisposable _writer;

        //todo(pekka): allow configuration
        private static readonly TimeSpan WriteMessageInterval = TimeSpan.FromMilliseconds(10);

        public WebSocketClient(WebSocket socket)
        {
            _socket = socket;
            _writer = Observable.Timer(TimeSpan.FromMilliseconds(0), WriteMessageInterval)
                .SubscribeAsync(_ => InternalWriteMessage(), OnWriteError, () => { });
        }

        private Task InternalWriteMessage()
        {
            //todo: should throw or not?
            if (_socket.CloseStatus.HasValue)
                return Task.CompletedTask;

            if (_writeQueue.TryDequeue(out var message))
            {
                return _socket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            return Task.CompletedTask;
        }

        private void OnWriteError(Exception error)
        {
           
        }

        public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

        public Task WriteMessageAsync(string message)
        {
            var messageSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            _writeQueue.Enqueue(messageSegment);
            return Task.CompletedTask;
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
            _writer.Dispose();
            
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

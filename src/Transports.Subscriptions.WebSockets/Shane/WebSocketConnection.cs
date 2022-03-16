#nullable enable

using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets;
using GraphQL.Transport;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane
{
    public class WebSocketConnection : IOperationMessageSendStream
    {
        private readonly WebSocket _webSocket;
        private readonly AsyncMessagePump<Message> _pump;
        private readonly IGraphQLSerializer _serializer;
        private readonly CancellationToken _cancellationToken;
        private readonly WebsocketWriterStream _stream;
        private readonly TaskCompletionSource<bool> _outputClosed = new();

        public WebSocketConnection(HttpContext httpContext, WebSocket webSocket, IGraphQLSerializer serializer)
        {
            _cancellationToken = (httpContext ?? throw new ArgumentNullException(nameof(httpContext))).RequestAborted;
            _cancellationToken.ThrowIfCancellationRequested();
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _stream = new(webSocket);
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _pump = new(SendMessageAsync);
        }

        public virtual async Task ExecuteAsync(IOperationMessageReceiveStream operationMessageReceiveStream)
        {
            if (operationMessageReceiveStream == null)
                throw new ArgumentNullException(nameof(operationMessageReceiveStream));
            try
            {
                var receiveStream = new MemoryStream();
                byte[] buffer = new byte[4096];
                var bufferMemory = new Memory<byte>(buffer);
                var bufferStream = new MemoryStream(buffer, false);
                while (true)
                {
                    var result = await _webSocket.ReceiveAsync(bufferMemory, _cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // prevent any more messages from being queued
                        operationMessageReceiveStream.Dispose();
                        // send a close request if none was sent yet
                        if (!_outputClosed.Task.IsCompleted)
                        {
                            // queue the closure
                            await CloseConnectionAsync();
                            // wait until the close has been sent
                            await _outputClosed.Task;
                        }
                        // quit
                        return;
                    }
                    if (result.EndOfMessage)
                    {
                        if (receiveStream.Length == 0)
                        {
                            if (result.Count == 0)
                                continue;
                            bufferStream.Position = 0;
                            var message = await _serializer.ReadAsync<OperationMessage>(bufferStream, _cancellationToken);
                            await operationMessageReceiveStream.OnMessageReceivedAsync(message);
                        }
                        else
                        {
                            receiveStream.Write(buffer, 0, result.Count);
                            receiveStream.Position = 0;
                            var message = await _serializer.ReadAsync<OperationMessage>(receiveStream, _cancellationToken);
                            receiveStream.SetLength(0);
                            await operationMessageReceiveStream.OnMessageReceivedAsync(message);
                        }
                    }
                    else
                    {
                        if (result.Count == 0)
                            continue;
                        receiveStream.Write(buffer, 0, result.Count);
                    }
                }
            }
            catch (WebSocketException)
            {
                return;
            }
            finally
            {
                // prevent any more messages from being sent
                _outputClosed.TrySetResult(false);
                // prevent any more messages from attempting to send
                operationMessageReceiveStream.Dispose();
            }
        }

        public Task CloseConnectionAsync()
            => CloseConnectionAsync(1000, null);

        public Task CloseConnectionAsync(int closeStatusId, string? closeDescription)
        {
            _pump.Post(new Message { CloseStatus = (WebSocketCloseStatus)closeStatusId, CloseDescription = closeDescription });
            return Task.CompletedTask;
        }

        public Task SendMessageAsync(OperationMessage message)
        {
            _pump.Post(new Message { OperationMessage = message });
            return Task.CompletedTask;
        }

        private async Task SendMessageAsync(Message message)
        {
            if (_outputClosed.Task.IsCompleted)
                return;
            if (message.OperationMessage != null)
            {
                await _serializer.WriteAsync(_stream, message.OperationMessage, _cancellationToken);
                await _stream.FlushAsync(_cancellationToken);
            }
            else
            {
                _outputClosed.TrySetResult(true);
                await _webSocket.CloseOutputAsync(message.CloseStatus, message.CloseDescription, _cancellationToken);
            }
        }

        private struct Message
        {
            public OperationMessage? OperationMessage;
            public WebSocketCloseStatus CloseStatus;
            public string? CloseDescription;
        }
    }
}

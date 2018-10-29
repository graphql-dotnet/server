using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebsocketWriterStream : Stream
    {
        private readonly WebSocket _webSocket;

        public WebsocketWriterStream(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Text, false,
                cancellationToken);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(new ArraySegment<byte>(Array.Empty<byte>()), WebSocketMessageType.Text, true, cancellationToken);
        }

        public override void Flush()
        {
            throw new System.NotImplementedException("Synchronous methods are not supported by WebsocketWriterStream.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException("Synchronous methods are not supported by WebsocketWriterStream.");
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}

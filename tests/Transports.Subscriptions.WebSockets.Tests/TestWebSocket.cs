using System.Net.WebSockets;

namespace GraphQL.Server.Transports.WebSockets.Tests;

public class TestWebSocket : WebSocket
{
    public TestWebSocket()
    {
        CurrentMessage = new ChunkedMemoryStream();
    }

    public override void Abort()
    {
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

    public override void Dispose()
    {
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) => throw new NotSupportedException();

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
        CancellationToken cancellationToken)
    {
        if (buffer.Array != null)
        {
            CurrentMessage.Write(buffer.Array, buffer.Offset, buffer.Count);
        }

        if (endOfMessage)
        {
            Messages.Add(CurrentMessage);
            CurrentMessage = new ChunkedMemoryStream();
        }

        return Task.CompletedTask;
    }

    internal List<ChunkedMemoryStream> Messages { get; } = new List<ChunkedMemoryStream>();
    private ChunkedMemoryStream CurrentMessage { get; set; }

    public override WebSocketCloseStatus? CloseStatus { get; }
    public override string CloseStatusDescription { get; } = "";
    public override string SubProtocol { get; } = "";
    public override WebSocketState State { get; } = WebSocketState.Open;
}

internal class ChunkedMemoryStream : Stream
{
    private readonly List<byte[]> _chunks = new List<byte[]>();
    private int _positionChunk;
    private int _positionOffset;
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override void Flush()
    {
    }

    public override long Length => _chunks.Sum(c => c.Length);

    public override long Position
    {
        get => _position;
        set
        {
            _position = value;

            _positionChunk = 0;

            while (_positionOffset != 0)
            {
                if (_positionChunk >= _chunks.Count)
                    throw new OverflowException();

                if (_positionOffset < _chunks[_positionChunk].Length)
                    return;

                _positionOffset -= _chunks[_positionChunk].Length;
                _positionChunk++;
            }
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int result = 0;
        while (count != 0 && _positionChunk != _chunks.Count)
        {
            int fromChunk = Math.Min(count, _chunks[_positionChunk].Length - _positionOffset);
            if (fromChunk != 0)
            {
                Array.Copy(_chunks[_positionChunk], _positionOffset, buffer, offset, fromChunk);
                offset += fromChunk;
                count -= fromChunk;
                result += fromChunk;
                _position += fromChunk;
            }

            _positionOffset = 0;
            _positionChunk++;
        }

        return result;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = 0;

        switch (origin)
        {
            case SeekOrigin.Begin:
                newPos = offset;
                break;
            case SeekOrigin.Current:
                newPos = Position + offset;
                break;
            case SeekOrigin.End:
                newPos = Length - offset;
                break;
        }

        Position = Math.Max(0, Math.Min(newPos, Length));
        return newPos;
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        while (count != 0 && _positionChunk != _chunks.Count)
        {
            int toChunk = Math.Min(count, _chunks[_positionChunk].Length - _positionOffset);
            if (toChunk != 0)
            {
                Array.Copy(buffer, offset, _chunks[_positionChunk], _positionOffset, toChunk);
                offset += toChunk;
                count -= toChunk;
                _position += toChunk;
            }

            _positionOffset = 0;
            _positionChunk++;
        }

        if (count != 0)
        {
            byte[] chunk = new byte[count];
            Array.Copy(buffer, offset, chunk, 0, count);
            _chunks.Add(chunk);
            _positionChunk = _chunks.Count;
            _position += count;
        }
    }

    public byte[] ToArray()
    {
        using (var ms = new MemoryStream())
        {
            foreach (byte[] bytes in _chunks)
            {
                ms.Write(bytes, 0, bytes.Length);
            }
            return ms.ToArray();
        }
    }
}

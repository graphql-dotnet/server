namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// A readable memory stream based on a buffer, with a resettable maximum length.
/// </summary>
internal class ReusableMemoryReaderStream : Stream
{
    private readonly byte[] _buffer;
    private int _position;
    private int _length;

    /// <summary>
    /// Initializes a new instance based on the specified buffer.
    /// </summary>
    public ReusableMemoryReaderStream(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => _length;

    /// <inheritdoc/>
    public override long Position
    {
        get => _position;
        set => _position = Math.Max(Math.Min(checked((int)value), _length), 0);
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
        => Read(new Span<byte>(buffer, offset, count));

    /// <inheritdoc/>
    public
#if !NETSTANDARD2_0
        override
#endif
        int Read(Span<byte> buffer)
    {
        var count = Math.Min(_length - _position, buffer.Length);
        var source = new Span<byte>(_buffer, _position, count);
        _position += count;
        source.CopyTo(buffer);
        return count;
    }

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => Task.FromResult(Read(buffer, offset, count));

#if !NETSTANDARD2_0
    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => new(Read(buffer.Span));
#endif

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
        => Position =
            origin == SeekOrigin.Begin ? offset :
            origin == SeekOrigin.Current ? offset + _position :
            origin == SeekOrigin.End ? offset + _length :
            throw new ArgumentOutOfRangeException(nameof(origin));

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        _length = checked((int)Math.Max(Math.Min(value, _buffer.Length), 0));
        if (_position > _length)
            _position = _length;
    }

    /// <summary>
    /// Sets the length to the specified value and resets the position to the start of the stream.
    /// </summary>
    public void ResetLength(int value)
    {
        _length = Math.Max(Math.Min(value, _buffer.Length), 0);
        _position = 0;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int ReadByte()
    {
        if (_position == _length)
            return -1;
        return _buffer[_position++];
    }
}

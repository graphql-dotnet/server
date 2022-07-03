namespace Tests.WebSockets;

public class ReusableMemoryReaderStreamTests
{
    private readonly byte[] _buffer = new byte[] { 1, 2, 3, 4, 5 };
    private readonly ReusableMemoryReaderStream _stream;

    public ReusableMemoryReaderStreamTests()
    {
        _stream = new ReusableMemoryReaderStream(_buffer);
    }

    [Fact]
    public void Constructor()
    {
        Should.Throw<ArgumentNullException>(() => new ReusableMemoryReaderStream(null!));
    }

    [Fact]
    public void Props()
    {
        _stream.CanRead.ShouldBeTrue();
        _stream.CanSeek.ShouldBeTrue();
        _stream.CanWrite.ShouldBeFalse();
    }

    [Fact]
    public void Length()
    {
        _stream.Length.ShouldBe(0);
        _stream.SetLength(3);
        _stream.Length.ShouldBe(3);
        _stream.SetLength(0);
        _stream.Length.ShouldBe(0);
        _stream.SetLength(5);
        _stream.Length.ShouldBe(5);
        _stream.Position = 3;
        _stream.SetLength(7);
        _stream.Length.ShouldBe(5);
        _stream.Position.ShouldBe(3);
        _stream.SetLength(-1);
        _stream.Length.ShouldBe(0);
        _stream.Position.ShouldBe(0);
    }

    [Fact]
    public void Position()
    {
        _stream.SetLength(3);
        _stream.Position.ShouldBe(0);
        _stream.Position = 2;
        _stream.Position.ShouldBe(2);
        _stream.Position = 0;
        _stream.Position.ShouldBe(0);
        _stream.Position = 3;
        _stream.Position.ShouldBe(3);
        _stream.Position = 5;
        _stream.Position.ShouldBe(3);
        _stream.Position = -1;
        _stream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task Flush()
    {
        Should.Throw<NotSupportedException>(() => _stream.Flush());
        await Should.ThrowAsync<NotSupportedException>(() => _stream.FlushAsync(default));
    }

    [Fact]
    public async Task Read()
    {
        var buf = new byte[5];
        _stream.SetLength(3);
        _stream.Position = 1;
        _stream.Read(buf, 2, 3).ShouldBe(2);
        _stream.Position.ShouldBe(3);
        buf.ShouldBe(new byte[] { 0, 0, 2, 3, 0 });

        buf = new byte[5];
        _stream.Position = 1;
        _stream.Read(buf).ShouldBe(2);
        buf.ShouldBe(new byte[] { 2, 3, 0, 0, 0 });

        _stream.Position = 1;
        _stream.ReadByte().ShouldBe(2);
        _stream.ReadByte().ShouldBe(3);
        _stream.ReadByte().ShouldBe(-1);

#if !NET48
        buf = new byte[5];
        var mem = new Memory<byte>(buf);
        _stream.Position = 1;
        (await _stream.ReadAsync(mem)).ShouldBe(2);
        _stream.Position.ShouldBe(3);
        buf.ShouldBe(new byte[] { 2, 3, 0, 0, 0 });
#endif

        buf = new byte[5];
        _stream.Position = 1;
        (await _stream.ReadAsync(buf, 2, 3)).ShouldBe(2);
        _stream.Position.ShouldBe(3);
        buf.ShouldBe(new byte[] { 0, 0, 2, 3, 0 });

        _buffer[2] = 30;

        buf = new byte[5];
        _stream.Position = 1;
        (await _stream.ReadAsync(buf, 2, 3, default)).ShouldBe(2);
        _stream.Position.ShouldBe(3);
        buf.ShouldBe(new byte[] { 0, 0, 2, 30, 0 });

        buf = new byte[5];
        _stream.SetLength(5);
        _stream.Position = 0;
        _stream.Read(buf, 1, 3).ShouldBe(3);
        _stream.Position.ShouldBe(3);
        buf.ShouldBe(new byte[] { 0, 1, 2, 30, 0 });
    }

    [Fact]
    public void Seek()
    {
        _stream.SetLength(5);
        _stream.Position.ShouldBe(0);
        _stream.Seek(1, SeekOrigin.Begin);
        _stream.Position.ShouldBe(1);
        _stream.Seek(2, SeekOrigin.Current);
        _stream.Position.ShouldBe(3);
        _stream.Seek(-4, SeekOrigin.End);
        _stream.Position.ShouldBe(1);
        Should.Throw<ArgumentOutOfRangeException>(() => _stream.Seek(0, (SeekOrigin)100));
    }

    [Fact]
    public void ResetLength()
    {
        _stream.Length.ShouldBe(0);
        _stream.ResetLength(3);
        _stream.Length.ShouldBe(3);
        _stream.Position.ShouldBe(0);
        _stream.Position = 1;
        _stream.ResetLength(0);
        _stream.Length.ShouldBe(0);
        _stream.Position.ShouldBe(0);
        _stream.Position = 1;
        _stream.ResetLength(5);
        _stream.Length.ShouldBe(5);
        _stream.Position.ShouldBe(0);
        _stream.Position = 1;
        _stream.ResetLength(7);
        _stream.Length.ShouldBe(5);
        _stream.Position.ShouldBe(0);
        _stream.Position = 1;
        _stream.ResetLength(-1);
        _stream.Length.ShouldBe(0);
        _stream.Position.ShouldBe(0);
    }

    [Fact]
    public async Task NotSupported()
    {
        Should.Throw<NotSupportedException>(() => _stream.Write(new byte[1], 0, 1));
#if !NET48
        Should.Throw<NotSupportedException>(() => _stream.Write(new Span<byte>(new byte[1], 0, 1)));
#endif
        await Should.ThrowAsync<NotSupportedException>(() => _stream.WriteAsync(new byte[1], 0, 1));
        await Should.ThrowAsync<NotSupportedException>(() => _stream.WriteAsync(new byte[1], 0, 1, default));
#if !NET48
        await Should.ThrowAsync<NotSupportedException>(async () => await _stream.WriteAsync(new Memory<byte>(new byte[1], 0, 1)));
#endif
        Should.Throw<NotSupportedException>(() => _stream.Flush());
        await Should.ThrowAsync<NotSupportedException>(() => _stream.FlushAsync());
        await Should.ThrowAsync<NotSupportedException>(() => _stream.FlushAsync(default));
    }

    [Fact]
    public async Task CopyTo()
    {
        _stream.SetLength(5);
        _stream.Position = 1;
        var s = new MemoryStream();
        _stream.CopyTo(s);
        s.ToArray().ShouldBe(new byte[] { 2, 3, 4, 5 });

        _stream.SetLength(5);
        _stream.Position = 1;
        s = new MemoryStream();
        await _stream.CopyToAsync(s);
        s.ToArray().ShouldBe(new byte[] { 2, 3, 4, 5 });

#if !NET48
        _stream.SetLength(5);
        _stream.Position = 1;
        s = new MemoryStream();
        await _stream.CopyToAsync(s, default(CancellationToken));
        s.ToArray().ShouldBe(new byte[] { 2, 3, 4, 5 });
#endif

        _stream.SetLength(5);
        _stream.Position = 1;
        s = new MemoryStream();
        await _stream.CopyToAsync(s, 100);
        s.ToArray().ShouldBe(new byte[] { 2, 3, 4, 5 });

        _stream.SetLength(5);
        _stream.Position = 1;
        s = new MemoryStream();
        await _stream.CopyToAsync(s, 100, default);
        s.ToArray().ShouldBe(new byte[] { 2, 3, 4, 5 });
    }
}

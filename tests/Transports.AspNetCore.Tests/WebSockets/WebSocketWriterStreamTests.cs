using System.Net.WebSockets;

namespace Tests.WebSockets;

public class WebSocketWriterStreamTests
{
    private readonly Mock<WebSocket> _mockWebSocket = new(MockBehavior.Strict);
    private readonly WebSocketWriterStream _stream;

    public WebSocketWriterStreamTests()
    {
        _stream = new(_mockWebSocket.Object);
    }

    [Fact]
    public async Task WriteAsync_1()
    {
        var buffer = new byte[1024];
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(buffer, 1, 2), WebSocketMessageType.Text, false, default))
            .Returns(Task.CompletedTask)
            .Verifiable();
        await _stream.WriteAsync(buffer, 1, 2);
        _mockWebSocket.Verify();
        _mockWebSocket.VerifyNoOtherCalls();
    }

#if !NET48
    [Fact]
    public async Task WriteAsync_2()
    {
        var buffer = new byte[1024];
        var memory = new ReadOnlyMemory<byte>(buffer);
        _mockWebSocket.Setup(x => x.SendAsync(memory, WebSocketMessageType.Text, false, default))
            .Returns(default(ValueTask))
            .Verifiable();
        await _stream.WriteAsync(memory);
        _mockWebSocket.Verify();
        _mockWebSocket.VerifyNoOtherCalls();
    }
#endif

    [Fact]
    public void Write()
    {
        var buffer = new byte[1024];
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(buffer, 1, 2), WebSocketMessageType.Text, false, default))
            .Returns(Task.CompletedTask).Verifiable();
        _stream.Write(buffer, 1, 2);
        _mockWebSocket.Verify();
        _mockWebSocket.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FlushAsync()
    {
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(Array.Empty<byte>(), 0, 0), WebSocketMessageType.Text, true, default))
            .Returns(Task.CompletedTask).Verifiable();
        await _stream.FlushAsync();
        _mockWebSocket.Verify();
        _mockWebSocket.VerifyNoOtherCalls();
    }

    [Fact]
    public void Flush()
    {
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(Array.Empty<byte>(), 0, 0), WebSocketMessageType.Text, true, default))
            .Returns(Task.CompletedTask).Verifiable();
        _stream.Flush();
        _mockWebSocket.Verify();
        _mockWebSocket.VerifyNoOtherCalls();
    }

    [Fact]
    public void NotSupportedMethods()
    {
        Should.Throw<NotSupportedException>(() => _stream.Read(Array.Empty<byte>(), 0, 0));
        Should.Throw<NotSupportedException>(() => _stream.Seek(0, SeekOrigin.Begin));
        Should.Throw<NotSupportedException>(() => _stream.SetLength(0));
        Should.Throw<NotSupportedException>(() => _stream.Length);
        Should.Throw<NotSupportedException>(() => _stream.Position);
        Should.Throw<NotSupportedException>(() => _stream.Position = 0);
        _stream.CanRead.ShouldBeFalse();
        _stream.CanSeek.ShouldBeFalse();
        _stream.CanWrite.ShouldBeTrue();
    }


}

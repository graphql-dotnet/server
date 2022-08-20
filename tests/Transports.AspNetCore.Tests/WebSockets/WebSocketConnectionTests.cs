using System.Net.WebSockets;
#if NET48
using ValueWebSocketReceiveResult = System.Net.WebSockets.WebSocketReceiveResult;
#endif

namespace Tests.WebSockets;

public class WebSocketConnectionTests : IDisposable
{
    private readonly Mock<WebSocket> _mockWebSocket = new Mock<WebSocket>(MockBehavior.Strict);
    private WebSocket _webSocket => _mockWebSocket.Object;
    private readonly Mock<IGraphQLSerializer> _mockSerializer = new Mock<IGraphQLSerializer>(MockBehavior.Strict);
    private IGraphQLSerializer _serializer => _mockSerializer.Object;
    private readonly GraphQLHttpMiddlewareOptions _options = new();
    private readonly CancellationTokenSource _cts = new();
    private CancellationToken _token => _cts.Token;
    private Mock<TestWebSocketConnection> _mockConnection;
    private TestWebSocketConnection _connection => _mockConnection.Object;
    private readonly Queue<(byte[], ValueWebSocketReceiveResult, Task?)> _webSocketResponses = new();
    private readonly Mock<HttpContext> _mockHttpContext = new Mock<HttpContext>(MockBehavior.Strict);

    public WebSocketConnectionTests()
    {
        _mockConnection = new Mock<TestWebSocketConnection>(_mockHttpContext.Object, _webSocket, _serializer, _options, _token);
    }

    public void Dispose()
    {
        _mockWebSocket.Verify();
        _mockSerializer.Verify();
        _cts.Cancel();
        _cts.Dispose();
    }

    private void SetupWebSocketReceive(byte[] data, ValueWebSocketReceiveResult result, bool verifyCloseOutput = true, Task? sendWhen = null)
    {
        _webSocketResponses.Enqueue((data, result, sendWhen));
#if NET48
        _mockWebSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), _token))
            .Returns<ArraySegment<byte>, CancellationToken>(async (bytes, _) =>
#else
        _mockWebSocket.Setup(x => x.ReceiveAsync(It.IsAny<Memory<byte>>(), _token))
            .Returns<Memory<byte>, CancellationToken>(async (bytes, _) =>
#endif
            {
                if (_webSocketResponses.Count == 0)
                    throw new InvalidOperationException("No more data");
                var result = _webSocketResponses.Dequeue();
                if (result.Item3 != null)
                    await result.Item3;
                new Memory<byte>(result.Item1).CopyTo(bytes);
#if NET48
                return result.Item2;
#else
                return result.Item2;
#endif
            })
            .Verifiable();
        if (verifyCloseOutput)
        {
            _mockConnection.Protected().Setup<Task>("OnCloseOutputAsync", WebSocketCloseStatus.NormalClosure, ItExpr.IsNull<string>())
                .Returns(Task.CompletedTask)
                .Verifiable();
        }
    }

    [Fact]
    public void Constructor()
    {
        var context = Mock.Of<HttpContext>(MockBehavior.Strict);
        Should.Throw<ArgumentNullException>(() => new WebSocketConnection(null!, _webSocket, _serializer, _options.WebSockets, default));
        Should.Throw<ArgumentNullException>(() => new WebSocketConnection(context, null!, _serializer, _options.WebSockets, default));
        Should.Throw<ArgumentNullException>(() => new WebSocketConnection(context, _webSocket, null!, _options.WebSockets, default));
        Should.Throw<ArgumentNullException>(() => new WebSocketConnection(context, _webSocket, _serializer, null!, default));
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(2147483648d)]
    public void Constructor_InvalidTimeout(double ms)
    {
        _options.WebSockets.DisconnectionTimeout = TimeSpan.FromMilliseconds(ms);
        Should.Throw<ArgumentOutOfRangeException>(() => new WebSocketConnection(Mock.Of<HttpContext>(MockBehavior.Strict), _webSocket, _serializer, _options.WebSockets, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2147483647d)]
    public void Constructor_ValidTimeout(double ms)
    {
        _options.WebSockets.DisconnectionTimeout = TimeSpan.FromMilliseconds(ms);
        _ = new WebSocketConnection(Mock.Of<HttpContext>(MockBehavior.Strict), _webSocket, _serializer, _options.WebSockets, default);
    }

    [Fact]
    public async Task ExecuteAsync_NullCheck()
    {
        _mockConnection.CallBase = true;
        await Should.ThrowAsync<ArgumentNullException>(() => _connection.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_CanOnlyRunOnce()
    {
        _mockConnection.CallBase = true;
        _ = _connection.ExecuteAsync(Mock.Of<IOperationMessageProcessor>(MockBehavior.Strict));
        await Should.ThrowAsync<InvalidOperationException>(()
            => _connection.ExecuteAsync(Mock.Of<IOperationMessageProcessor>(MockBehavior.Strict)));
        _cts.Cancel();
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(5000, 1000, true)]
    [InlineData(1000, 5000, false)]
    public async Task ExecuteAsync_DisconnectTimeout(int timeoutMs, int delayMs, bool closesOutput)
    {
        _options.WebSockets.DisconnectionTimeout = TimeSpan.FromMilliseconds(timeoutMs);
        _mockConnection = new Mock<TestWebSocketConnection>(_mockHttpContext.Object, _webSocket, _serializer, _options, _token);

        var delay1 = new TaskCompletionSource<bool>();

        _mockConnection.CallBase = true;
        var message = new OperationMessage();
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        // upon initialization, send a message
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(async () =>
        {
            await _mockConnection.Object.SendMessageAsync(message);
            delay1.SetResult(true);
        }).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();

        _mockSerializer.Setup(x => x.WriteAsync<OperationMessage?>(It.IsAny<Stream>(), message, _token))
            .Returns<Stream, OperationMessage, CancellationToken>((stream, _, _) =>
            {
                return stream.WriteAsync(new byte[] { 1, 2, 3 }, 0, 3, _token);
            }).Verifiable();
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(new byte[] { 1, 2, 3 }), WebSocketMessageType.Text, false, _token))
            .Returns(Task.CompletedTask).Verifiable();
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(new byte[] { }), WebSocketMessageType.Text, true, _token))
            .Returns(Task.Delay(delayMs)).Verifiable();
        if (closesOutput)
        {
            _mockWebSocket.Setup(x => x.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, _token))
                .Returns(Task.CompletedTask).Verifiable();
        }

        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true), false, delay1.Task);

        await _connection.ExecuteAsync(mockReceiveStream.Object);
        mockReceiveStream.Verify();
    }

    [Fact]
    public async Task ExecuteAsync_DecodeSmallMessage()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message2)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 4, 5 }, new ValueWebSocketReceiveResult(2, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 3)
                {
                    buf.ShouldBe(new byte[] { 1, 2, 3 });
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 4, 5 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_MessagesNotProcessedAfterClose()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(() =>
        {
            return _connection.CloseAsync();
        }).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 4, 5 }, new ValueWebSocketReceiveResult(2, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 3)
                {
                    buf.ShouldBe(new byte[] { 1, 2, 3 });
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 4, 5 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DecodeMultipartMessage()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message2)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(new byte[] { 4, 5 }, new ValueWebSocketReceiveResult(2, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 6 }, new ValueWebSocketReceiveResult(1, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 5)
                {
                    buf.ShouldBe(new byte[] { 1, 2, 3, 4, 5 });
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 6 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DecodeLargeMultipartMessage()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message2)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(DummyData(16300), new ValueWebSocketReceiveResult(16300, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(DummyData(84), new ValueWebSocketReceiveResult(84, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(DummyData(500), new ValueWebSocketReceiveResult(500, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 6 }, new ValueWebSocketReceiveResult(1, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 16884)
                {
                    buf.ShouldBe(DummyData(16300).Concat(DummyData(84)).Concat(DummyData(500)).ToArray());
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 6 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DecodeLargeMultipartMessage_ZeroLastMessage()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message2)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(DummyData(16300), new ValueWebSocketReceiveResult(16300, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(DummyData(84), new ValueWebSocketReceiveResult(84, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(DummyData(500), new ValueWebSocketReceiveResult(500, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 6 }, new ValueWebSocketReceiveResult(1, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 16884)
                {
                    buf.ShouldBe(DummyData(16300).Concat(DummyData(84)).Concat(DummyData(500)).ToArray());
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 6 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    private static byte[] DummyData(int length)
    {
        var data = new byte[length];
        byte j = (byte)(length % 256);
        for (int i = 0; i < length; i++)
            data[i] = j;
        return data;
    }

    [Fact]
    public async Task ExecuteAsync_SkipZeroLengthMessages()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                StreamToArray(stream).ShouldBe(new byte[] { 1, 2, 3 });
                return new ValueTask<OperationMessage?>(message);
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SkipNullMessages()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                StreamToArray(stream).ShouldBe(new byte[] { 1, 2, 3 });
                return default;
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SkipNullMessages_Multipart()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                StreamToArray(stream).ShouldBe(new byte[] { 1, 2, 3 });
                return default;
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SkipZeroLength_Multipart()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message2)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3 }, new ValueWebSocketReceiveResult(3, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(new byte[] { 4, 5 }, new ValueWebSocketReceiveResult(2, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 6 }, new ValueWebSocketReceiveResult(1, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 5)
                {
                    buf.ShouldBe(new byte[] { 1, 2, 3, 4, 5 });
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 6 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DecodeMultipartMessage_ZeroLastMessage()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        var message = new OperationMessage();
        var message2 = new OperationMessage();
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message2)).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose()).Verifiable();
        SetupWebSocketReceive(new byte[] { 1, 2, 3, 4, 5 }, new ValueWebSocketReceiveResult(5, WebSocketMessageType.Text, false));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { 6 }, new ValueWebSocketReceiveResult(1, WebSocketMessageType.Text, true));
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true));
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage?>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((stream, _) =>
            {
                var buf = StreamToArray(stream);
                if (buf.Length == 5)
                {
                    buf.ShouldBe(new byte[] { 1, 2, 3, 4, 5 });
                    return new ValueTask<OperationMessage?>(message);
                }
                else
                {
                    buf.ShouldBe(new byte[] { 6 });
                    return new ValueTask<OperationMessage?>(message2);
                }
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WebSocket_Canceled()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose());
#if NET48
        _mockWebSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), _token))
            .Returns<ArraySegment<byte>, CancellationToken>((_, token) =>
#else
        _mockWebSocket.Setup(x => x.ReceiveAsync(It.IsAny<Memory<byte>>(), _token))
            .Returns<Memory<byte>, CancellationToken>((_, token) =>
#endif
            {
                _cts.Cancel();
                token.ThrowIfCancellationRequested();
#if NET48
                return Task.FromResult<ValueWebSocketReceiveResult>(null!);
#else
                return default;
#endif
            })
            .Verifiable();
        await Should.ThrowAsync<OperationCanceledException>(() => _connection.ExecuteAsync(mockReceiveStream.Object));
    }

    [Fact]
    public async Task ExecuteAsync_WebSocket_EatsWebSocketExceptions()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose());
#if NET48
        _mockWebSocket.Setup(x => x.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), _token))
            .Returns<ArraySegment<byte>, CancellationToken>((_, _) =>
#else
        _mockWebSocket.Setup(x => x.ReceiveAsync(It.IsAny<Memory<byte>>(), _token))
            .Returns<Memory<byte>, CancellationToken>((_, _) =>
#endif
            {
                _cts.Cancel();
                throw new WebSocketException();
            })
            .Verifiable();
        await _connection.ExecuteAsync(mockReceiveStream.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Serializer_Canceled()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose());
        SetupWebSocketReceive(new byte[] { 1, 2, 3, 4, 5 }, new ValueWebSocketReceiveResult(5, WebSocketMessageType.Text, true), false);
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((_, token) =>
            {
                _cts.Cancel();
                token.ThrowIfCancellationRequested();
                return default;
            })
            .Verifiable();
        await Should.ThrowAsync<OperationCanceledException>(() => _connection.ExecuteAsync(mockReceiveStream.Object));
    }

    [Fact]
    public async Task ExecuteAsync_Serializer_Canceled_Multipart()
    {
        _mockConnection.CallBase = true;
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.InitializeConnectionAsync()).Returns(Task.CompletedTask).Verifiable();
        mockReceiveStream.Setup(x => x.Dispose());
        SetupWebSocketReceive(new byte[] { 1, 2, 3, 4, 5 }, new ValueWebSocketReceiveResult(5, WebSocketMessageType.Text, false), false);
        SetupWebSocketReceive(new byte[] { }, new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, true), false);
        _mockSerializer.Setup(x => x.ReadAsync<OperationMessage>(It.IsAny<Stream>(), _token))
            .Returns<Stream, CancellationToken>((_, token) =>
            {
                _cts.Cancel();
                token.ThrowIfCancellationRequested();
                return default;
            })
            .Verifiable();
        await Should.ThrowAsync<OperationCanceledException>(() => _connection.ExecuteAsync(mockReceiveStream.Object));
    }

    [Fact]
    public async Task CloseConnectionAsync()
    {
        _mockConnection.Protected().Setup<Task>("OnCloseOutputAsync", WebSocketCloseStatus.NormalClosure, ItExpr.IsNull<string>())
            .Returns(Task.CompletedTask).Verifiable();
        await _connection.CloseAsync();
        _mockConnection.Verify();
    }

    [Fact]
    public async Task CloseConnectionAsync_Specific()
    {
        _mockConnection.Protected().Setup<Task>("OnCloseOutputAsync", WebSocketCloseStatus.InternalServerError, "test")
            .Returns(Task.CompletedTask).Verifiable();
        await _connection.CloseAsync(1011, "test");
        _mockConnection.Verify();
    }

    [Fact]
    public async Task SendMessageAsync()
    {
        var message = new OperationMessage();
        _mockConnection.Protected().Setup<Task>("OnSendMessageAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        await _connection.SendMessageAsync(message);
        _mockConnection.Verify();
    }

    [Fact]
    public async Task LastMessageSentAt()
    {
        var oldTime = _connection.LastMessageSentAt;
        await Task.Delay(100);
        var message = new OperationMessage();
        _mockConnection.Protected().Setup<Task>("OnSendMessageAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        await _connection.SendMessageAsync(message);
        _mockConnection.Verify();
        var newTime = DateTime.UtcNow;
        _connection.LastMessageSentAt.ShouldBeGreaterThan(oldTime);
        _connection.LastMessageSentAt.ShouldBeLessThanOrEqualTo(newTime);
    }

    [Fact]
    public async Task DoNotSendMessagesAfterOutputIsClosed()
    {
        // send a message
        var message = new OperationMessage();
        _mockConnection.Protected().SetupGet<TimeSpan>("DefaultDisconnectionTimeout").CallBase().Verifiable();
        _mockConnection.Protected().Setup<Task>("OnSendMessageAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        _mockConnection.Protected().Setup<Task>("OnCloseOutputAsync", WebSocketCloseStatus.NormalClosure, ItExpr.IsNull<string>())
            .Returns(Task.CompletedTask).Verifiable();
        await _connection.SendMessageAsync(message);
        // close the output
        await _connection.CloseAsync();
        // send another message -- OnSendMessageAsync should not be called for the new message
        await _connection.SendMessageAsync(new OperationMessage());
        _mockConnection.Verify();
        _mockConnection.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnDispatchMessageAsync()
    {
        var message = new OperationMessage();
        var mockReceiveStream = new Mock<IOperationMessageProcessor>(MockBehavior.Strict);
        mockReceiveStream.Setup(x => x.OnMessageReceivedAsync(message))
            .Returns(Task.CompletedTask).Verifiable();
        _mockConnection.CallBase = true;
        await _connection.Do_OnDispatchMessageAsync(mockReceiveStream.Object, message);
        mockReceiveStream.Verify();
    }

    [Fact]
    public async Task OnSendMessageAsync()
    {
        var message = new OperationMessage();
        _mockSerializer.Setup(x => x.WriteAsync(It.IsAny<WebSocketWriterStream>(), message, _token))
            .Returns(Task.CompletedTask).Verifiable();
        _mockWebSocket.Setup(x => x.SendAsync(new ArraySegment<byte>(Array.Empty<byte>()), WebSocketMessageType.Text, true, _token))
            .Returns(Task.CompletedTask).Verifiable();
        _mockConnection.CallBase = true;
        await _connection.Do_OnSendMessageAsync(message);
        _mockSerializer.Verify();
        _mockWebSocket.Verify();
    }

    [Fact]
    public async Task OnCloseOutputAsync()
    {
        _mockWebSocket.Setup(x => x.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "abcd", _token))
            .Returns(Task.CompletedTask).Verifiable();
        _mockConnection.CallBase = true;
        await _connection.Do_OnCloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "abcd");
        _mockWebSocket.Verify();
    }

    private static byte[] StreamToArray(Stream stream)
    {
        var s = new MemoryStream();
        stream.CopyTo(s);
        return s.ToArray();
    }
}

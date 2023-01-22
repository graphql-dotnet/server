namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Manages a WebSocket connection, dispatching messages to the specified <see cref="IOperationMessageProcessor"/>,
/// and sending messages requested by the <see cref="IOperationMessageProcessor"/> implementation.
/// <br/><br/>
/// The <see cref="ExecuteAsync(IOperationMessageProcessor)"/> method may only be executed once for each
/// instance. Awaiting the result will return once the WebSocket connection has been properly closed from both
/// ends, after all messages have been sent.
/// <br/><br/>
/// Calls to <see cref="IOperationMessageProcessor.OnMessageReceivedAsync(OperationMessage)"/> will be awaited
/// before dispatching subsequent messages.
/// <br/><br/>
/// Calls to <see cref="WebSocketConnection{T}.CloseAsync()"/> and <see cref="WebSocketConnection{T}.SendMessageAsync(T)"/> may be
/// called on multiple threads simultaneously.  They are queued for delivery and sent in the order posted.
/// Messages posted after requesting the connection be closed will be discarded.
/// </summary>
public class WebSocketConnection : WebSocketConnection<OperationMessage>, IWebSocketConnection
{
    private readonly IGraphQLSerializer _serializer;
    private IOperationMessageProcessor? _operationMessageProcessor;

    /// <summary>
    /// Returns the default disconnection timeout value.
    /// See <see cref="GraphQLWebSocketOptions.DisconnectionTimeout"/>.
    /// </summary>
    protected virtual TimeSpan DefaultDisconnectionTimeout { get; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public override CancellationToken RequestAborted { get; }

    /// <summary>
    /// Initializes an instance with the specified parameters.
    /// </summary>
    public WebSocketConnection(HttpContext httpContext, WebSocket webSocket, IGraphQLSerializer serializer, GraphQLWebSocketOptions options, CancellationToken requestAborted)
        : base(httpContext, webSocket)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (options.DisconnectionTimeout.HasValue)
        {
            if ((options.DisconnectionTimeout.Value != Timeout.InfiniteTimeSpan && options.DisconnectionTimeout.Value.TotalMilliseconds < 0) || options.DisconnectionTimeout.Value.TotalMilliseconds > int.MaxValue)
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentOutOfRangeException(nameof(options) + "." + nameof(GraphQLWebSocketOptions.DisconnectionTimeout));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        DisconnectionTimeout = options.DisconnectionTimeout ?? DefaultDisconnectionTimeout;
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        RequestAborted = requestAborted;
    }

    /// <inheritdoc/>
    protected override void OnClosing()
    {
        // prevent any more messages from being queued
        _operationMessageProcessor?.Dispose();
    }

    /// <inheritdoc/>
    protected override ValueTask<OperationMessage?> DeserializeAsync(Stream stream)
        => _serializer.ReadAsync<OperationMessage>(stream, RequestAborted);

    /// <inheritdoc/>
    protected override Task SerializeAsync(Stream stream, OperationMessage message)
        => _serializer.WriteAsync(stream, message, RequestAborted);

    /// <summary>
    /// Listens to incoming messages on the WebSocket specified in the constructor,
    /// dispatching the messages to the specified <paramref name="operationMessageProcessor"/>.
    /// Returns or throws <see cref="OperationCanceledException"/> when the WebSocket connection is closed.
    /// </summary>
    public virtual Task ExecuteAsync(IOperationMessageProcessor operationMessageProcessor)
    {
        _operationMessageProcessor = operationMessageProcessor ?? throw new ArgumentNullException(nameof(operationMessageProcessor));
        return ExecuteAsync();
    }

    /// <summary>
    /// Dispatches a received message to an <see cref="IOperationMessageProcessor"/> instance.
    /// Override if logging is desired.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before dispatching another message.
    /// </summary>
    protected override Task OnDispatchMessageAsync(OperationMessage message)
    {
        if (_operationMessageProcessor != null)
            return OnDispatchMessageAsync(_operationMessageProcessor, message);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches a received message the specified <see cref="IOperationMessageProcessor"/> instance.
    /// Override if logging is desired.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before dispatching another message.
    /// </summary>
    protected virtual Task OnDispatchMessageAsync(IOperationMessageProcessor operationMessageProcessor, OperationMessage message)
        => operationMessageProcessor.OnMessageReceivedAsync(message);
}

/// <summary>
/// Manages a WebSocket connection, dispatching messages via <see cref="WebSocketConnection{T}.OnDispatchMessageAsync(T)"/>,
/// and sending messages requested by calls to <see cref="WebSocketConnection{T}.OnSendMessageAsync(T)"/>.
/// <br/><br/>
/// The <see cref="ExecuteAsync()"/> method may only be executed once for each
/// instance. Awaiting the result will return once the WebSocket connection has been properly closed from both
/// ends, after all messages have been sent.
/// <br/><br/>
/// Calls to <see cref="WebSocketConnection{T}.OnDispatchMessageAsync(T)"/> will be awaited
/// before dispatching subsequent messages.
/// <br/><br/>
/// Calls to <see cref="CloseAsync()"/> and <see cref="SendMessageAsync(T)"/> may be
/// called on multiple threads simultaneously.  They are queued for delivery and sent in the order posted.
/// Messages posted after requesting the connection be closed will be discarded.
/// </summary>
public abstract class WebSocketConnection<T> : IDisposable
{
    private readonly WebSocket _webSocket;
    private readonly AsyncMessagePump<Message> _pump;
    private readonly WebSocketWriterStream _stream;
    private readonly TaskCompletionSource<bool> _outputClosed = new();
    private volatile bool _closeRequested;
    private TimeSpan _closeTimeout => DisconnectionTimeout;
    private int _executed;
    private const int BUFFER_SIZE = 16384;

    /// <inheritdoc cref="HttpContext.RequestAborted"/>
    public virtual CancellationToken RequestAborted => HttpContext.RequestAborted;

    /// <summary>
    /// Returns the default disconnection timeout value.
    /// See <see cref="GraphQLWebSocketOptions.DisconnectionTimeout"/>.
    /// </summary>
    protected virtual TimeSpan DisconnectionTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public DateTime LastMessageSentAt { get; private set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Initializes an instance with the specified parameters.
    /// </summary>
    public WebSocketConnection(HttpContext httpContext, WebSocket webSocket)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _stream = new(webSocket);
        _pump = new(HandleOutgoingMessageAsync);
    }

    /// <summary>
    /// Listens to incoming messages on the WebSocket specified in the constructor,
    /// dispatching the messages via <see cref="OnDispatchMessageAsync(T)"/>.
    /// Returns or throws <see cref="OperationCanceledException"/> when the WebSocket connection is closed.
    /// </summary>
    protected async Task ExecuteAsync()
    {
        if (Interlocked.Exchange(ref _executed, 1) == 1)
            throw new InvalidOperationException($"{nameof(ExecuteAsync)} may only be called once per instance.");
        bool receivedCloseMessage = false;
        try
        {
            // set up a buffer in case a message is longer than 16KB
            var receiveStream = new MemoryStream();
            // set up a 16KB block to accumulate data from WebSocket
            byte[] buffer = new byte[BUFFER_SIZE];
            int bufferOffset = 0;
            // prep a reader stream - just a view of the 16KB buffer for the deserializer so the data does not need to be copied to the memory stream
            var bufferStream = new ReusableMemoryReaderStream(buffer);
            // read messages until an exception occurs, the cancellation token is signaled, or a 'close' message is received
            while (true)
            {
                // prep a Memory instance pointing to the free part of block
#if NETSTANDARD2_0
                var bufferMemory = new ArraySegment<byte>(buffer, bufferOffset, BUFFER_SIZE - bufferOffset);
                WebSocketReceiveResult result;
#else
                var bufferMemory = new Memory<byte>(buffer, bufferOffset, BUFFER_SIZE - bufferOffset);
                ValueWebSocketReceiveResult result;
#endif
                try
                {
                    result = await _webSocket.ReceiveAsync(bufferMemory, RequestAborted);
                }
                catch (OperationCanceledException) when (RequestAborted.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (RequestAborted.IsCancellationRequested)
                {
                    throw new OperationCanceledException(null, ex, RequestAborted);
                }
                bufferOffset += result.Count;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    receivedCloseMessage = true;
                    OnClosing();
                    // send a close request if none was sent yet and the socket has not yet been closed
                    if (!_outputClosed.Task.IsCompleted && !RequestAborted.IsCancellationRequested)
                    {
                        // queue the closure
                        _ = CloseAsync();
                        // wait until the close has been sent
                        await Task.WhenAny(
                            _outputClosed.Task,
                            Task.Delay(_closeTimeout, RequestAborted));
                    }
                    // quit after the close request was fulfilled
                    return;
                }
                // if close has been requested, ignore incoming messages
                if (_closeRequested)
                {
                    bufferOffset = 0;
                    continue;
                }
                // if this is the last block terminating a message
                if (result.EndOfMessage)
                {
                    // if message was less then 16 KB
                    if (receiveStream.Length == 0)
                    {
                        // if the message is empty, skip to the next message
                        if (bufferOffset == 0)
                            continue;
                        // read the message
                        bufferStream.ResetLength(bufferOffset);
                        var message = await DeserializeAsync(bufferStream);
                        bufferOffset = 0;
                        // dispatch the message
                        if (message != null)
                            await OnDispatchMessageAsync(message);
                    }
                    else
                    {
                        // if there is any data in byte[] buffer, flush it into stream buffer
                        if (bufferOffset > 0)
                            receiveStream.Write(buffer, 0, bufferOffset);
                        bufferOffset = 0;
                        // read the message from the buffer
                        receiveStream.Position = 0;
                        var message = await DeserializeAsync(bufferStream);
                        // clear the buffer
                        receiveStream.SetLength(0);
                        // dispatch the message
                        if (message != null)
                            await OnDispatchMessageAsync(message);
                    }
                }
                else
                {
                    // if there is room in the buffer for more information, continue to fill the buffer
                    if (bufferOffset < BUFFER_SIZE)
                        continue;
                    // if there is any data in byte[] buffer, flush it into stream buffer
                    if (bufferOffset > 0)
                        receiveStream.Write(buffer, 0, bufferOffset);
                    bufferOffset = 0;
                }
            }
        }
        finally
        {
            // prevent any more messages from being sent
            _outputClosed.TrySetResult(false);
            OnClosing();
            var sentCloseMessage = _outputClosed.Task.Result;
            if (!receivedCloseMessage || !sentCloseMessage)
                await OnNonGracefulShutdownAsync(receivedCloseMessage, sentCloseMessage);
        }
    }

    /// <summary>
    /// Asynchronously deserializes the specified stream to the specified object type.
    /// Typically used to parse <see cref="OperationMessage"/> instances from JSON.
    /// </summary>
    protected abstract ValueTask<T?> DeserializeAsync(Stream stream);

    /// <summary>
    /// Asynchronously serializes the specified object type to the specified stream.
    /// Typically used to serialize <see cref="OperationMessage"/> instances to JSON.
    /// </summary>
    protected abstract Task SerializeAsync(Stream stream, T message);

    /// <inheritdoc/>
    /// <remarks>
    /// The close message is posted to a queue and execution returns immediately.
    /// </remarks>
    public Task CloseAsync()
        => CloseAsync(1000, null);

    /// <inheritdoc/>
    /// <remarks>
    /// The close message is posted to a queue and execution returns immediately.
    /// </remarks>
    public Task CloseAsync(int eventId, string? description)
    {
        _closeRequested = true;
        _pump.Post(new Message { CloseStatus = (WebSocketCloseStatus)eventId, CloseDescription = description });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The message is posted to a queue and execution returns immediately.
    /// </remarks>
    public Task SendMessageAsync(T message)
    {
        _pump.Post(new Message { OperationMessage = message });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the next <see cref="Message"/> in the queue, which contains either an <see cref="OperationMessage"/>
    /// or <see cref="WebSocketCloseStatus"/> with description, passing to either
    /// <see cref="OnSendMessageAsync(T)"/> or <see cref="OnCloseOutputAsync(WebSocketCloseStatus, string?)"/>.
    /// <br/><br/>
    /// The methods <see cref="SendMessageAsync(T)"/>, <see cref="CloseAsync()"/>
    /// and <see cref="CloseAsync(int, string?)"/> add <see cref="Message"/> instances to the queue.
    /// </summary>
    private async Task HandleOutgoingMessageAsync(Message message)
    {
        // Messages posted after requesting the connection be closed will be discarded.
        if (_outputClosed.Task.IsCompleted)
            return;
        LastMessageSentAt = DateTime.UtcNow;
        if (message.OperationMessage != null)
        {
            await OnSendMessageAsync(message.OperationMessage);
        }
        else
        {
            await OnCloseOutputAsync(message.CloseStatus, message.CloseDescription);
            _outputClosed.TrySetResult(true);
        }
    }

    /// <summary>
    /// Indicates that future outgoing messages will be discarded as the WebSocket is in
    /// the process of closing.
    /// </summary>
    protected abstract void OnClosing();

    /// <summary>
    /// Dispatches a received message.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before dispatching another message.
    /// </summary>
    protected abstract Task OnDispatchMessageAsync(T message);

    /// <summary>
    /// Sends the specified message to the underlying <see cref="WebSocket"/>.
    /// Override if logging is desired.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before sending another message or closing the output stream.
    /// </summary>
    protected virtual async Task OnSendMessageAsync(T message)
    {
        await SerializeAsync(_stream, message);
        await _stream.FlushAsync(RequestAborted);
    }

    /// <summary>
    /// Closes the underlying <see cref="WebSocket"/>.
    /// Override if logging is desired.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before sending another message or closing the output stream.
    /// </summary>
    protected virtual Task OnCloseOutputAsync(WebSocketCloseStatus closeStatus, string? closeDescription)
        => _webSocket.CloseOutputAsync(closeStatus, closeDescription, RequestAborted);

    /// <summary>
    /// A queue entry; see <see cref="HandleOutgoingMessageAsync(Message)"/>.
    /// </summary>
    /// <param name="OperationMessage">The message to send, if set; if it is <see langword="null"/> then this is a closure message.</param>
    /// <param name="CloseStatus">The close status.</param>
    /// <param name="CloseDescription">The close description.</param>
    private record struct Message(T? OperationMessage, WebSocketCloseStatus CloseStatus, string? CloseDescription);

    /// <summary>
    /// Occurs when a WebSocket connection is terminated before both the input and output were closed.
    /// Override if logging is desired.
    /// </summary>
    protected virtual Task OnNonGracefulShutdownAsync(bool receivedCloseMessage, bool sentCloseMessage)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

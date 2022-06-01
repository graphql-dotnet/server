using System.Net.WebSockets;
using GraphQL.Transport;
using Microsoft.AspNetCore.Http;

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
/// Calls to <see cref="CloseAsync()"/> and <see cref="SendMessageAsync(OperationMessage)"/> may be
/// called on multiple threads simultaneously.  They are queued for delivery and sent in the order posted.
/// Messages posted after requesting the connection be closed will be discarded.
/// </summary>
public class WebSocketConnection : IWebSocketConnection
{
    private readonly WebSocket _webSocket;
    private readonly AsyncMessagePump<Message> _pump;
    private readonly IGraphQLSerializer _serializer;
    private readonly WebSocketWriterStream _stream;
    private readonly TaskCompletionSource<bool> _outputClosed = new();
    private readonly TimeSpan _closeTimeout;
    private int _executed;

    /// <inheritdoc/>
    public CancellationToken RequestAborted { get; }

    /// <summary>
    /// Returns the default disconnection timeout value.
    /// See <see cref="GraphQLWebSocketOptions.DisconnectionTimeout"/>.
    /// </summary>
    protected virtual TimeSpan DefaultDisconnectionTimeout { get; } = TimeSpan.FromSeconds(10);

    /// <inheritdoc/>
    public DateTime LastMessageSentAt { get; private set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Initializes an instance with the specified parameters.
    /// </summary>
    public WebSocketConnection(HttpContext httpContext, WebSocket webSocket, IGraphQLSerializer serializer, GraphQLHttpMiddlewareOptions options, CancellationToken requestAborted)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (options.WebSockets.DisconnectionTimeout.HasValue)
        {
            if ((options.WebSockets.DisconnectionTimeout.Value != Timeout.InfiniteTimeSpan && options.WebSockets.DisconnectionTimeout.Value.TotalMilliseconds < 0) || options.WebSockets.DisconnectionTimeout.Value.TotalMilliseconds > int.MaxValue)
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentOutOfRangeException(nameof(options) + "." + nameof(GraphQLWebSocketOptions.DisconnectionTimeout));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        _closeTimeout = options.WebSockets.DisconnectionTimeout ?? DefaultDisconnectionTimeout;
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _stream = new(webSocket);
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _pump = new(HandleOutgoingMessageAsync);
        RequestAborted = requestAborted;
    }

    /// <summary>
    /// Listens to incoming messages on the WebSocket specified in the constructor,
    /// dispatching the messages to the specified <paramref name="operationMessageProcessor"/>.
    /// Returns or throws <see cref="OperationCanceledException"/> when the WebSocket connection is closed.
    /// </summary>
    public virtual async Task ExecuteAsync(IOperationMessageProcessor operationMessageProcessor)
    {
        if (operationMessageProcessor == null)
            throw new ArgumentNullException(nameof(operationMessageProcessor));
        if (Interlocked.Exchange(ref _executed, 1) == 1)
            throw new InvalidOperationException($"{nameof(ExecuteAsync)} may only be called once per instance.");
        bool receivedCloseMessage = false;
        try
        {
            await operationMessageProcessor.InitializeConnectionAsync();
            // set up a buffer in case a message is longer than one block
            var receiveStream = new MemoryStream();
            // set up a 16KB data block
            byte[] buffer = new byte[16384];
            // prep a Memory instance pointing to the block
#if NETSTANDARD2_0
            var bufferMemory = new ArraySegment<byte>(buffer);
#else
            var bufferMemory = new Memory<byte>(buffer);
#endif
            // prep a reader stream
            var bufferStream = new ReusableMemoryReaderStream(buffer);
            // read messages until an exception occurs, the cancellation token is signaled, or a 'close' message is received
            while (true)
            {
                var result = await _webSocket.ReceiveAsync(bufferMemory, RequestAborted);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    receivedCloseMessage = true;
                    // prevent any more messages from being queued
                    operationMessageProcessor.Dispose();
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
                // if this is the last block terminating a message
                if (result.EndOfMessage)
                {
                    // if only one block of data was sent for this message
                    if (receiveStream.Length == 0)
                    {
                        // if the message is empty, skip to the next message
                        if (result.Count == 0)
                            continue;
                        // read the message
                        bufferStream.ResetLength(result.Count);
                        var message = await _serializer.ReadAsync<OperationMessage>(bufferStream, RequestAborted);
                        // dispatch the message
                        if (message != null)
                            await OnDispatchMessageAsync(operationMessageProcessor, message);
                    }
                    else
                    {
                        // if there is any data in this block, add it to the buffer
                        if (result.Count > 0)
                            receiveStream.Write(buffer, 0, result.Count);
                        // read the message from the buffer
                        receiveStream.Position = 0;
                        var message = await _serializer.ReadAsync<OperationMessage>(receiveStream, RequestAborted);
                        // clear the buffer
                        receiveStream.SetLength(0);
                        // dispatch the message
                        if (message != null)
                            await OnDispatchMessageAsync(operationMessageProcessor, message);
                    }
                }
                else
                {
                    // if there is any data in this block, add it to the buffer
                    if (result.Count > 0)
                        receiveStream.Write(buffer, 0, result.Count);
                }
            }
        }
        catch (WebSocketException) when (RequestAborted.IsCancellationRequested)
        {
        }
        finally
        {
            // prevent any more messages from being sent
            _outputClosed.TrySetResult(false);
            // prevent any more messages from attempting to send
            // note: this statement should be redundant, as WebSocketHandler should dispose operationMessageProcessor
            operationMessageProcessor.Dispose();
            var sentCloseMessage = _outputClosed.Task.Result;
            if (!receivedCloseMessage || !sentCloseMessage)
                await OnNonGracefulShutdownAsync(receivedCloseMessage, sentCloseMessage);
        }
    }

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
        _pump.Post(new Message { CloseStatus = (WebSocketCloseStatus)eventId, CloseDescription = description });
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The message is posted to a queue and execution returns immediately.
    /// </remarks>
    public Task SendMessageAsync(OperationMessage message)
    {
        _pump.Post(new Message { OperationMessage = message });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the next <see cref="Message"/> in the queue, which contains either an <see cref="OperationMessage"/>
    /// or <see cref="WebSocketCloseStatus"/> with description, passing to either
    /// <see cref="OnSendMessageAsync(OperationMessage)"/> or <see cref="OnCloseOutputAsync(WebSocketCloseStatus, string?)"/>.
    /// <br/><br/>
    /// The methods <see cref="SendMessageAsync(OperationMessage)"/>, <see cref="CloseAsync()"/>
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
    /// Dispatches a received message to an <see cref="IOperationMessageProcessor"/> instance.
    /// Override if logging is desired.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before dispatching another message.
    /// </summary>
    protected virtual Task OnDispatchMessageAsync(IOperationMessageProcessor operationMessageProcessor, OperationMessage message)
        => operationMessageProcessor.OnMessageReceivedAsync(message);

    /// <summary>
    /// Sends the specified message to the underlying <see cref="WebSocket"/>.
    /// Override if logging is desired.
    /// <br/><br/>
    /// This method is synchronized and will wait until completion before sending another message or closing the output stream.
    /// </summary>
    protected virtual async Task OnSendMessageAsync(OperationMessage message)
    {
        await _serializer.WriteAsync(_stream, message, RequestAborted);
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
    private record struct Message(OperationMessage? OperationMessage, WebSocketCloseStatus CloseStatus, string? CloseDescription);

    /// <summary>
    /// Occurs when a WebSocket connection is terminated before both the input and output were closed.
    /// Override if logging is desired.
    /// </summary>
    protected virtual Task OnNonGracefulShutdownAsync(bool receivedCloseMessage, bool sentCloseMessage)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public virtual void Dispose()
    {
    }
}

namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Represents a WebSocket connection, dispatching received messages over
/// the connection to the specified <see cref="IOperationMessageProcessor"/>,
/// and sending requested messages out the connection when requested.
/// <br/><br/>
/// <see cref="ExecuteAsync(IOperationMessageProcessor)"/> may be only called once.
/// All members must be thread-safe.
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    /// <summary>
    /// Listens to incoming messages over the WebSocket connection,
    /// dispatching the messages to the specified <paramref name="operationMessageProcessor"/>.
    /// Returns or throws <see cref="OperationCanceledException"/> when the WebSocket connection is closed.
    /// </summary>
    Task ExecuteAsync(IOperationMessageProcessor operationMessageProcessor);

    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    Task SendMessageAsync(OperationMessage message);

    /// <summary>
    /// Performs a graceful shutdown of the WebSocket connection with event ID 1000, and
    /// prevents further incoming messages from being dispatched through <see cref="IOperationMessageProcessor"/>.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    /// Performs a graceful shutdown of the WebSocket connection with the specified error information, and
    /// prevents further incoming messages from being dispatched through <see cref="IOperationMessageProcessor"/>.
    /// </summary>
    Task CloseAsync(int eventId, string? description);

    /// <summary>
    /// Returns the last UTC time that a message was sent to the client.
    /// This property is needed to implement the idle timeout to only send a keep-alive message after ## seconds of "no traffic". 
    /// </summary>
    DateTime LastMessageSentAt { get; }

    /// <inheritdoc cref="HttpContext.RequestAborted"/>
    CancellationToken RequestAborted { get; }

    /// <summary>
    /// Returns the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> associated with this request.
    /// </summary>
    HttpContext HttpContext { get; }
}

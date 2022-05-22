using GraphQL.Transport;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Represents a WebSocket connection, dispatching received messages over
/// the connection to the specified <see cref="IOperationMessageProcessor"/>,
/// and sending requested messages out the connection when requested.
/// <br/><br/>
/// <see cref="ExecuteAsync(IOperationMessageProcessor)"/> may be only called once.
/// All members must be thread-safe.
/// </summary>
public interface IWebSocketConnection
{
    /// <summary>
    /// Listens to incoming messages over the WebSocket connection,
    /// dispatching the messages to the specified <paramref name="operationMessageProcessor"/>.
    /// Returns or throws <see cref="OperationCanceledException"/> when the WebSocket connection is closed.
    /// </summary>
    Task ExecuteAsync(IOperationMessageProcessor operationMessageProcessor);

    /// <summary>
    /// Sends a message.
    /// </summary>
    Task SendMessageAsync(OperationMessage message);

    /// <summary>
    /// Closes the WebSocket connection.
    /// </summary>
    Task CloseConnectionAsync();

    /// <summary>
    /// Closes the WebSocket connection with the specified error information.
    /// </summary>
    Task CloseConnectionAsync(int eventId, string? description);

    /// <summary>
    /// Returns the last UTC time that a message was sent.
    /// </summary>
    DateTime LastMessageSentAt { get; }

    /// <inheritdoc cref="HttpContext.RequestAborted"/>
    CancellationToken RequestAborted { get; }

    /// <summary>
    /// Returns the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> associated with this request.
    /// </summary>
    HttpContext HttpContext { get; }
}

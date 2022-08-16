namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Processes a stream of messages received from a WebSockets client.
/// <see cref="IDisposable.Dispose"/> must be called when the WebSocket connection
/// has received a close message, and/or when the connection terminates.
/// Methods defined within this interface need not be thread-safe.
/// </summary>
public interface IOperationMessageProcessor : IDisposable
{
    /// <summary>
    /// Starts the connection initialization timer, if configured.
    /// </summary>
    Task InitializeConnectionAsync();

    /// <summary>
    /// Called when a message is received from the client.
    /// </summary>
    Task OnMessageReceivedAsync(OperationMessage message);
}

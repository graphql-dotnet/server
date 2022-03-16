using System.Threading.Tasks;
using GraphQL.Transport;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane
{
    /// <summary>
    /// Must be thread-safe.
    /// </summary>
    public interface IOperationMessageSendStream
    {
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
    }
}

#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Threading.Tasks;
using GraphQL.Transport;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane
{
    /// <summary>
    /// Represents a stream of messages received from a WebSockets client.
    /// All public methods must be thread-safe.
    /// </summary>
    public interface IOperationMessageReceiveStream : IDisposable
    {
        /// <summary>
        /// Starts the connection initialization timer, if configured.
        /// </summary>
        void StartConnectionInitTimer();

        /// <summary>
        /// Called when a message is received from the client.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        Task OnMessageReceivedAsync(OperationMessage message);
    }
}

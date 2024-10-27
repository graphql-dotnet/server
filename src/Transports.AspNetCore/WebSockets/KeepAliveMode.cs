namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Specifies the mode of keep-alive behavior.
/// </summary>
public enum KeepAliveMode
{
    /// <summary>
    /// Same as <see cref="Timeout"/>: Sends a unidirectional keep-alive message when no message has been received within the specified timeout period.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Sends a unidirectional keep-alive message when no message has been received within the specified timeout period.
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// Sends a unidirectional keep-alive message at a fixed interval, regardless of message activity.
    /// </summary>
    Interval = 2,

    /// <summary>
    /// Sends a Ping message with a payload after the specified timeout from the last received Pong,
    /// and waits for a corresponding Pong response. Requires that the client reflects the payload
    /// in the response. Forcibly disconnects the client if the client does not respond with a Pong
    /// message within the specified timeout. This means that a dead connection will be closed after
    /// a maximum of double the <see cref="GraphQLWebSocketOptions.KeepAliveTimeout"/> period.
    /// </summary>
    /// <remarks>
    /// This mode is particularly useful when backpressure causes subscription messages to be delayed
    /// due to a slow or unresponsive client connection. The server can detect that the client is not
    /// processing messages in a timely manner and disconnect the client to free up resources.
    /// </remarks>
    TimeoutWithPayload = 3,
}

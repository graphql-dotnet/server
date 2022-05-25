namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Configuration options for a WebSocket connection.
/// </summary>
public class GraphQLWebSocketOptions
{
    /// <summary>
    /// The amount of time to wait for a GraphQL initialization packet before the connection is closed.
    /// The default is 10 seconds.
    /// </summary>
    public TimeSpan? ConnectionInitWaitTimeout { get; set; }

    /// <summary>
    /// The amount of time to wait between sending keep-alive packets.
    /// The default is <see langword="null"/> which means that keep-alive packets are disabled.
    /// <br/><br/>
    /// Keep in mind that the 'subscription-transport-ws' implementation typically
    /// disconnects clients if a keep-alive packet has not been received for 20 seconds,
    /// when keep-alive packets are enabled, so it is recommended to keep the keep-alive
    /// packets disabled or use a value less than 20 seconds.
    /// </summary>
    public TimeSpan? KeepAliveTimeout { get; set; }

    /// <summary>
    /// The amount of time to wait to attempt a graceful teardown of the WebSockets protocol.
    /// The default is 10 seconds.
    /// </summary>
    public TimeSpan? DisconnectionTimeout { get; set; }

    /// <summary>
    /// Disconnects a subscription from the client if the subscription source dispatches an
    /// <see cref="IObserver{T}.OnError(Exception)"/> event.  The default value is <see langword="true"/>.
    /// </summary>
    public bool DisconnectAfterErrorEvent { get; set; } = true;

    /// <summary>
    /// Disconnects a subscription from the client there are any GraphQL errors during a subscription.
    /// </summary>
    public bool DisconnectAfterAnyError { get; set; }
}

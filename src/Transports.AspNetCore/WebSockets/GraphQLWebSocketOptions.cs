namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Configuration options for a WebSocket connection.
/// </summary>
public class GraphQLWebSocketOptions
{
    /// <summary>
    /// The amount of time to wait for a GraphQL initialization packet before the connection is closed.
    /// A value of <see langword="null"/> indicates the default value defined by the implementation.
    /// The included implementations in this library have a default value of 10 seconds.
    /// </summary>
    public TimeSpan? ConnectionInitWaitTimeout { get; set; }

    /// <summary>
    /// The amount of time to wait between sending keep-alive packets.
    /// A value of <see cref="Timeout.InfiniteTimeSpan"/> means that keep-alive packets are disabled.
    /// A value of <see langword="null"/> indicates the default value defined by the implementation.
    /// The included implementations in this library default to having keep-alive packets disabled.
    /// <br/><br/>
    /// Keep in mind that the 'subscription-transport-ws' implementation typically
    /// disconnects clients if a keep-alive packet has not been received for 20 seconds,
    /// when keep-alive packets are enabled, so it is recommended to keep the keep-alive
    /// packets disabled or use a value less than 20 seconds.
    /// </summary>
    public TimeSpan? KeepAliveTimeout { get; set; }

    /// <summary>
    /// The amount of time to wait to attempt a graceful teardown of the WebSockets protocol.
    /// A value of <see langword="null"/> indicates the default value defined by the implementation.
    /// The included implementations in this library have a default value of 10 seconds.
    /// </summary>
    public TimeSpan? DisconnectionTimeout { get; set; }

    /// <summary>
    /// Disconnects a subscription from the client if the subscription source dispatches an
    /// <see cref="IObserver{T}.OnError(Exception)"/> event.  The default value is <see langword="true"/>.
    /// </summary>
    public bool DisconnectAfterErrorEvent { get; set; } = true;

    /// <summary>
    /// Disconnects a subscription from the client in the event of any GraphQL errors during a subscription.  The default value is <see langword="false"/>.
    /// </summary>
    public bool DisconnectAfterAnyError { get; set; }
}

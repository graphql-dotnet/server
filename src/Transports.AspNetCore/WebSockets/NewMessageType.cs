namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Protocol message types defined in
/// <see href="https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md"/>
/// and
/// <see href="https://github.com/enisdenjo/graphql-ws/blob/master/src/common.ts"/>
/// </summary>
public class NewMessageType
{
    /// <summary>
    /// <para>
    /// Indicates that the client wants to establish a connection within the existing socket.
    /// This connection is not the actual WebSocket communication channel, but is rather a
    /// frame within it asking the server to allow future operation requests.
    /// </para>
    /// <para>
    /// The server must receive the connection initialisation message within the allowed waiting
    /// time specified in the connectionInitWaitTimeout parameter during the server setup.
    /// If the client does not request a connection within the allowed timeout, the server
    /// will close the socket with the event: 4408: Connection initialisation timeout.
    /// </para>
    /// <para>
    /// If the server receives more than one ConnectionInit message at any given time, the server
    /// will close the socket with the event 4429: Too many initialisation requests.
    /// </para>
    /// <code>
    /// interface ConnectionInitMessage {
    ///   type: 'connection_init';
    ///   payload?: Record&lt;string, unknown&gt;;
    /// }
    /// </code>
    /// </summary>
    public const string GQL_CONNECTION_INIT = "connection_init"; // Client -> Server

    /// <summary>
    ///     The server responds with this message to the GQL_CONNECTION_INIT from client, indicating the server accepted
    ///     the connection.
    /// </summary>
    public const string GQL_CONNECTION_ACK = "connection_ack"; // Server -> Client

    /// <summary>
    ///     Client sends this message to execute GraphQL operation
    ///     id: string : The id of the GraphQL operation to start
    ///     payload: Object:
    ///     query: string : GraphQL operation as string or parsed GraphQL document node
    ///     variables?: Object : Object with GraphQL variables
    ///     operationName?: string : GraphQL operation name
    /// </summary>
    public const string GQL_SUBSCRIBE = "subscribe"; // Client -> Server

    /// <summary>
    ///     The server sends this message to transfer the GraphQL execution result from the server to the client, this message
    ///     is a response for GQL_SUBSCRIBE message.
    ///     For each GraphQL operation send with GQL_SUBSCRIBE, the server will respond with at least one GQL_DATA message.
    ///     id: string : ID of the operation that was successfully set up
    ///     payload: Object :
    ///     data: any: Execution result
    ///     errors?: Error[] : Array of resolvers errors
    /// </summary>
    public const string GQL_NEXT = "next"; // Server -> Client

    /// <summary>
    ///     Server sends this message upon a failing operation, before the GraphQL execution, usually due to GraphQL validation
    ///     errors (resolver errors are part of GQL_DATA message, and will be added as errors array)
    ///     payload: ExecutionError[] : payload with the error attributed to the operation failing on the server
    ///     id: string : operation ID of the operation that failed on the server
    /// </summary>
    public const string GQL_ERROR = "error"; // Server -> Client

    /// <summary>
    ///     Server sends this message to indicate that a GraphQL operation is done, and no more data will arrive for the
    ///     specific operation.
    ///     Client sends this message in order to stop a running GraphQL operation execution (for example: unsubscribe)
    ///     id: string : operation ID
    /// </summary>
    public const string GQL_COMPLETE = "complete"; // Bidirectional

    /// <summary>
    ///     Client or Server sends this message to request a GQL_PONG packet from the recipient.
    /// </summary>
    public const string GQL_PING = "ping"; // Bidirectional

    /// <summary>
    ///     Client or Server sends this message in response to a GQL_PING packet from the recipient.
    /// </summary>
    public const string GQL_PONG = "pong"; // Bidirectional
}

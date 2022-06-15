#pragma warning disable IDE1006 // Naming Styles

namespace GraphQL.Server.Transports.AspNetCore.WebSockets.GraphQLWs;

/// <summary>
/// Protocol message types defined in
/// <see href="https://github.com/enisdenjo/graphql-ws/blob/master/PROTOCOL.md"/>
/// and
/// <see href="https://github.com/enisdenjo/graphql-ws/blob/master/src/common.ts"/>
/// </summary>
public static class MessageType
{
    /// <summary>
    /// <para>
    /// Indicates that the client wants to establish a connection within the existing socket.
    /// This connection is not the actual WebSocket communication channel, but is rather a
    /// frame within it asking the server to allow future operation requests.
    /// </para>
    /// <para>
    /// The server must receive the connection initialization message within the allowed waiting
    /// time specified in the <see cref="GraphQLWebSocketOptions.ConnectionInitWaitTimeout"/>
    /// parameter during the server setup.  If the client does not request a connection within
    /// the allowed timeout, the server will close the socket with the event:
    /// <i>4408: Connection initialization timeout</i>.
    /// </para>
    /// <para>
    /// If the server receives more than one <see cref="ConnectionInit"/> message at any
    /// given time, the server will close the socket with the event
    /// <i>4429: Too many initialization requests</i>.
    /// </para>
    /// <para>
    /// The client may use the optional <see cref="OperationMessage.Payload"/> field to transfer
    /// additional details about the connection.
    /// </para>
    /// </summary>
    public const string ConnectionInit = "connection_init"; // Client -> Server

    /// <summary>
    /// <para>
    /// Expected response to the <see cref="ConnectionInit"/> message from the client acknowledging
    /// a successful connection with the server.
    /// </para>
    /// <para>
    /// The server can use the optional <see cref="OperationMessage.Payload"/> field to transfer additional details about the connection.
    /// </para>
    /// <para>
    /// The client is now ready to request subscription operations.
    /// </para>
    /// </summary>
    public const string ConnectionAck = "connection_ack"; // Server -> Client

    /// <summary>
    /// <para>
    /// Requests an operation as a <see cref="GraphQLRequest"/> specified in <see cref="OperationMessage.Payload"/>.
    /// This message provides a unique ID within <see cref="OperationMessage.Id"/> to connect published messages
    /// to the operation requested by this message.
    /// </para>
    /// <para>
    /// If there is already an active subscriber for an operation matching the provided ID, regardless of the operation type,
    /// the server must close the socket immediately with the event <i>4409: Subscriber for &lt;unique-operation-id&gt; already exists</i>.
    /// </para>
    /// <para>
    /// The server needs only keep track of IDs for as long as the subscription is active.
    /// Once a client completes an operation, it is free to re-use that ID.
    /// </para>
    /// <para>
    /// Executing operations is allowed only after the server has acknowledged the connection through the
    /// <see cref="ConnectionAck"/> message; if the connection is not acknowledged, the socket will be
    /// closed immediately with the event <i>4401: Unauthorized</i>.
    /// </para>
    /// </summary>
    public const string Subscribe = "subscribe"; // Client -> Server

    /// <summary>
    /// <para>
    /// Operation execution result(s) from the source stream created by the binding <see cref="Subscribe"/> message.
    /// After all results have been emitted, the <see cref="Complete"/> message will follow indicating stream completion.
    /// </para>
    /// <para>
    /// The <see cref="OperationMessage.Id"/> must contain the unique ID of the subscription, and the
    /// <see cref="OperationMessage.Payload"/> must contain a <see cref="ExecutionResult"/> instance.
    /// </para>
    /// </summary>
    public const string Next = "next"; // Server -> Client

    /// <summary>
    /// <para>
    /// Operation execution error(s) in response to the <see cref="Subscribe"/> message.  This can occur before
    /// execution starts, usually due to validation errors, or during the execution of the request.  This message
    /// terminates the operation and no further messages will be sent.
    /// </para>
    /// <para>
    /// The <see cref="OperationMessage.Id"/> must contain the unique ID of the subscription, and the
    /// <see cref="OperationMessage.Payload"/> must contain a list/array of <see cref="ExecutionError"/> instances.
    /// </para>
    /// </summary>
    public const string Error = "error"; // Server -> Client

    /// <summary>
    /// <para>
    /// <b>Server -&gt; Client</b> indicates that the requested operation execution has completed.  If the server dispatched
    /// the <see cref="Error"/> message relative to the original <see cref="Subscribe"/> message,
    /// no <see cref="Complete"/> message will be emitted.
    /// </para>
    /// <para>
    /// <b>Client -&gt; Server</b> indicates that the client has stopped listening and wants to complete the subscription.
    /// No further events, relevant to the original subscription, should be sent through. Even if the client sent a
    /// <see cref="Complete"/> message for a single-result-operation before it resolved, the result should not be
    /// sent through once it does.
    /// </para>
    /// <para>
    /// Note: The asynchronous nature of the full-duplex connection means that a client can send a <see cref="Complete"/>
    /// message to the server even when messages are in-flight to the client, or when the server has itself completed the
    /// operation (via an <see cref="Error"/> or <see cref="Complete"/> message).  Both client and server must
    /// therefore be prepared to receive (and ignore) messages for operations that they consider already completed.
    /// </para>
    /// <para>
    /// The <see cref="OperationMessage.Id"/> must contain the unique ID of the subscription.
    /// </para>
    /// </summary>
    public const string Complete = "complete"; // Bidirectional

    /// <summary>
    /// <para>
    /// Client or Server sends this message to request a <see cref="Pong"/> packet from the recipient.
    /// </para>
    /// <para>
    /// Useful for detecting failed connections, displaying latency metrics or other types of network probing.
    /// </para>
    /// <para>
    /// A <see cref="Pong"/> must be sent in response from the receiving party as soon as possible.
    /// </para>
    /// <para>
    /// The <see cref="Ping"/> message can be sent at any time within the established socket.
    /// </para>
    /// <para>
    /// The optional <see cref="OperationMessage.Payload"/> field can be used to transfer additional details about the ping.
    /// </para>
    /// </summary>
    public const string Ping = "ping"; // Bidirectional

    /// <summary>
    /// <para>
    /// The response to the <see cref="Ping"/> message. Must be sent as soon as the <see cref="Ping"/> message is received.
    /// </para>
    /// <para>
    /// The <see cref="Pong"/> message can be sent at any time within the established socket.
    /// Furthermore, the <see cref="Pong"/> message may even be sent unsolicited as an unidirectional heartbeat.
    /// </para>
    /// <para>
    /// The optional <see cref="OperationMessage.Payload"/> field can be used to transfer additional details about the pong.
    /// </para>
    /// </summary>
    public const string Pong = "pong"; // Bidirectional
}

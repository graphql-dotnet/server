using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Types;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLEndPoint<TSchema> where TSchema : Schema
    {
        private readonly ILogger<GraphQLEndPoint<TSchema>> _log;
        private readonly ISubscriptionMessageProtocol<TSchema> _messagingProtocol;

        public GraphQLEndPoint(
            ISubscriptionMessageProtocol<TSchema> messagingProtocol,
            ILogger<GraphQLEndPoint<TSchema>> log)
        {
            _log = log;
            _messagingProtocol = messagingProtocol;
        }

        protected ConcurrentDictionary<string, GraphQLConnectionContext> Connections { get; } =
            new ConcurrentDictionary<string, GraphQLConnectionContext>();

        public async Task OnConnectedAsync(GraphQLConnectionContext connection)
        {
            AddConnection(connection);
            await WaitForMessagesAsync(connection);
            await CloseConnectionAsync(connection);
        }

        private void AddConnection(GraphQLConnectionContext connection)
        {
            if (!Connections.TryAdd(connection.ConnectionId, connection))
                throw new InvalidOperationException($"Connection '{connection.ConnectionId}' already is connected");
        }

        public async Task WaitForMessagesAsync(GraphQLConnectionContext connection)
        {
            while (!connection.CloseStatus.HasValue)
            {
                var operationMessage = await connection.Reader.ReadMessageAsync<OperationMessage>();

                if (operationMessage == null || connection.CloseStatus.HasValue)
                {
                    _log.LogWarning(
                        $"Connection: {connection.ConnectionId} was closed while reading messages");
                    break;
                }

                _log.LogDebug($"Received op: {operationMessage.Type}, opid: {operationMessage.Id}");
                await HandleMessageAsync(operationMessage, connection);
            }
        }

        public async Task CloseConnectionAsync(GraphQLConnectionContext connection)
        {
            Connections.TryRemove(connection.ConnectionId, out var _);
            await _messagingProtocol.HandleConnectionClosed(
                new OperationMessageContext(connection.ConnectionId,
                    connection.Writer, new OperationMessage
                    {
                        Type = MessageTypes.GQL_CONNECTION_TERMINATE
                    }));

            await connection.CloseAsync();
        }

        private Task HandleMessageAsync(OperationMessage op, GraphQLConnectionContext connection)
        {
            return _messagingProtocol.HandleMessageAsync(new OperationMessageContext(connection.ConnectionId,
                connection.Writer, op));
        }
    }
}

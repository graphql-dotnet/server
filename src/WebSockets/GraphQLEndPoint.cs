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
        private readonly ISubscriptionProtocolHandler<TSchema> _messagingProtocolHandler;

        public GraphQLEndPoint(
            ISubscriptionProtocolHandler<TSchema> messagingProtocolHandler,
            ILogger<GraphQLEndPoint<TSchema>> log)
        {
            _log = log;
            _messagingProtocolHandler = messagingProtocolHandler;
        }

        public ConcurrentDictionary<string, IConnectionContext> Connections { get; } =
            new ConcurrentDictionary<string, IConnectionContext>();

        public async Task OnConnectedAsync(IConnectionContext connection)
        {
            AddConnection(connection);
            await WaitForMessagesAsync(connection).ConfigureAwait(false);
        }

        private void AddConnection(IConnectionContext connection)
        {
            if (!Connections.TryAdd(connection.ConnectionId, connection))
                throw new InvalidOperationException($"Connection({connection.ConnectionId}): already is connected");
        }

        public async Task WaitForMessagesAsync(IConnectionContext connection)
        {
            while (!connection.CloseStatus.HasValue)
            {
                var operationMessage = await connection.Reader.ReadMessageAsync<OperationMessage>().ConfigureAwait(false);

                if (operationMessage == null)
                {
                    _log.LogWarning(
                        $"Connection({connection.ConnectionId}): received null message");
                    break;
                }

                _log.LogDebug($"Connection({connection.ConnectionId}): received op: {operationMessage.Type}, opid: {operationMessage.Id}");
                await HandleMessageAsync(operationMessage, connection).ConfigureAwait(false);
            }
        }

        public async Task CloseConnectionAsync(IConnectionContext connection)
        {
            Connections.TryRemove(connection.ConnectionId, out var _);
            await _messagingProtocolHandler.HandleConnectionClosed(
                new OperationMessageContext(connection.ConnectionId,
                    connection.Writer, new OperationMessage
                    {
                        Type = MessageTypes.GQL_CONNECTION_TERMINATE
                    })).ConfigureAwait(false);

            await connection.CloseAsync().ConfigureAwait(false);
        }

        private Task HandleMessageAsync(OperationMessage op, IConnectionContext connection)
        {
            return _messagingProtocolHandler.HandleMessageAsync(new OperationMessageContext(connection.ConnectionId,
                connection.Writer, op));
        }
    }
}

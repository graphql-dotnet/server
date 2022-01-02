using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnectionFactory<TSchema> : IWebSocketConnectionFactory<TSchema>
        where TSchema : ISchema
    {
        private static readonly Dictionary<WebSocketsSubprotocol, string> DataEventTypes = new()
        {
            [WebSocketsSubprotocol.GraphQLWs] = MessageType.GQL_DATA,
            [WebSocketsSubprotocol.GraphQLTransportWs] = MessageType.GQL_NEXT,
        };

        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGraphQLExecuter<TSchema> _executer;
        private readonly IEnumerable<IOperationMessageListener> _messageListeners;
        private readonly IDocumentWriter _documentWriter;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        [Obsolete]
        public WebSocketConnectionFactory(
            ILogger<WebSocketConnectionFactory<TSchema>> logger,
            ILoggerFactory loggerFactory,
            IGraphQLExecuter<TSchema> executer,
            IEnumerable<IOperationMessageListener> messageListeners,
            IDocumentWriter documentWriter)
            : this(logger, loggerFactory, executer, messageListeners, documentWriter, null)
        {
        }

        public WebSocketConnectionFactory(
            ILogger<WebSocketConnectionFactory<TSchema>> logger,
            ILoggerFactory loggerFactory,
            IGraphQLExecuter<TSchema> executer,
            IEnumerable<IOperationMessageListener> messageListeners,
            IDocumentWriter documentWriter,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _executer = executer;
            _messageListeners = messageListeners;
            _documentWriter = documentWriter;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public WebSocketConnection CreateConnection(WebSocket socket, string connectionId, WebSocketsSubprotocol subprotocol)
        {
            _logger.LogDebug("Creating server for connection {connectionId} with {subprotocol}", connectionId, subprotocol);

            var transport = new WebSocketTransport(socket, _documentWriter);
            var manager = _serviceScopeFactory != null
                ? new SubscriptionManager(_executer, _loggerFactory, _serviceScopeFactory, DataEventTypes[subprotocol])
#pragma warning disable CS0612 // Type or member is obsolete
                : new SubscriptionManager(_executer, _loggerFactory, DataEventTypes[subprotocol]);
#pragma warning restore CS0612 // Type or member is obsolete
            var server = new SubscriptionServer(
                transport,
                manager,
                _messageListeners,
                _loggerFactory.CreateLogger<SubscriptionServer>()
            );

            return new WebSocketConnection(transport, server);
        }
    }
}

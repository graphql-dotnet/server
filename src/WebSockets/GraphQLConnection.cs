using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLEndPoint<TSchema> where TSchema : Schema
    {
        private readonly TSchema _schema;
        private readonly ISubscriptionExecuter _subscriptionExecuter;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly IDocumentWriter _documentWriter;
        private readonly ILogger<GraphQLEndPoint<TSchema>> _log;

        protected ConcurrentBag<SubscriptionHandle> Subscriptions { get; } = new ConcurrentBag<SubscriptionHandle>();

        public GraphQLEndPoint(
            TSchema schema,
            ISubscriptionExecuter subscriptionExecuter,
            IDocumentExecuter documentExecuter,
            IDocumentWriter documentWriter,
            ILogger<GraphQLEndPoint<TSchema>> log)
        {
            _schema = schema;
            _subscriptionExecuter = subscriptionExecuter;
            _documentExecuter = documentExecuter;
            _documentWriter = documentWriter;
            _log = log;
        }

        public Task OnConnectedAsync(GraphQLConnectionContext connection)
        {
            return WaitForMessagesAsync(connection);
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
                    await connection.CloseAsync();
                    break;
                }

                _log.LogDebug($"Received op: {operationMessage.Type}, opid: {operationMessage.Id}");
                await HandleMessageAsync(operationMessage, connection);
            }

            await connection.CloseAsync();
        }

        private Task HandleMessageAsync(OperationMessage op, GraphQLConnectionContext connection)
        {
            switch (op.Type)
            {
                case MessageTypes.GQL_CONNECTION_INIT:
                    return HandleConnectionInitAsync(op, connection);
                case MessageTypes.GQL_START:
                    return HandleStartAsync(op, connection);
                default: return Task.CompletedTask;
            }
        }

        private async Task HandleStartAsync(OperationMessage op, GraphQLConnectionContext connection)
        {
            _log.LogInformation($"Starting subscription {op.Id}");
            var query = op.Payload.ToObject<GraphQuery>();
            var stream = await SubscribeAsync(query);
            Subscriptions.Add(new SubscriptionHandle(op, stream, connection, _documentWriter));
            _log.LogInformation($"Subscription: {op.Id} started");
        }

        private async Task<IObservable<object>> SubscribeAsync(GraphQuery query)
        {
            var result = await _subscriptionExecuter.SubscribeAsync(new ExecutionOptions
            {
                Schema = _schema,
                OperationName = query.OperationName,
                Inputs = query.GetInputs(),
                Query = query.Query
            });

            return result.Streams.Values.Single();
        }

        private Task HandleConnectionInitAsync(OperationMessage op, GraphQLConnectionContext connection)
        {
            _log.LogInformation($"Acknowleding GraphQL connection: {op}");
            return WriteConnectionAckAsync(connection);
        }

        private Task WriteConnectionAckAsync(GraphQLConnectionContext connection)
        {
            return connection.Writer.WriteMessageAsync(new OperationMessage
            {
                Type = MessageTypes.GQL_CONNECTION_ACK
            });
        }
    }
}

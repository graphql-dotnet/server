using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionMessageProtocol<TSchema> : ISubscriptionMessageProtocol<TSchema> where TSchema : Schema
    {
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ILogger<SubscriptionMessageProtocol<TSchema>> _log;
        private readonly TSchema _schema;
        private readonly ISubscriptionExecuter _subscriptionExecuter;


        public SubscriptionMessageProtocol(
            TSchema schema,
            ISubscriptionExecuter subscriptionExecuter,
            IDocumentExecuter documentExecuter,
            ILogger<SubscriptionMessageProtocol<TSchema>> log)
        {
            _schema = schema;
            _subscriptionExecuter = subscriptionExecuter;
            _documentExecuter = documentExecuter;
            _log = log;
        }

        public ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionHandle>> Subscriptions { get; } =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionHandle>>();

        public Task HandleMessageAsync(OperationMessageContext context)
        {
            switch (context.Op.Type)
            {
                case MessageTypes.GQL_CONNECTION_INIT:
                    return HandleConnectionInitAsync(context);
                case MessageTypes.GQL_START:
                    return HandleStartAsync(context);
                case MessageTypes.GQL_STOP:
                    return HandleStopAsync(context);
                case MessageTypes.GQL_CONNECTION_TERMINATE:
                    return HandleTerminateAsync(context);
                default: return Task.CompletedTask;
            }
        }

        /// <inheritdoc />
        public Task HandleConnectionClosed(OperationMessageContext context)
        {
            return HandleTerminateAsync(context);
        }

        protected Task HandleTerminateAsync(OperationMessageContext context)
        {
            if (Subscriptions.TryRemove(context.ConnectionId, out var subscriptions))
            {
                foreach (var subscription in subscriptions.Values)
                    subscription.Dispose();

                subscriptions.Clear();
            }

            return Task.CompletedTask;
        }

        protected Task HandleStopAsync(OperationMessageContext context)
        {
            if (Subscriptions.TryGetValue(context.ConnectionId, out var subscriptions))
                if (subscriptions.TryRemove(context.Op.Id, out var subscriptionHandle))
                    subscriptionHandle.Dispose();

            return Task.CompletedTask;
        }

        protected async Task HandleStartAsync(OperationMessageContext context)
        {
            var query = context.Op.Payload.ToObject<GraphQuery>();
            var result = await SubscribeAsync(query);

            if (result.Errors?.Any() == true)
            {
                await WriteOperationErrorsAsync(context, result);
                return;
            }

            var stream = result.Streams.Values.Single();
            Subscriptions.AddOrUpdate(context.ConnectionId, connectionId =>
            {
                var subscriptions = new ConcurrentDictionary<string, SubscriptionHandle>();
                subscriptions.TryAdd(context.Op.Id,
                    new SubscriptionHandle(context.Op, stream, context.MessageWriter, new DocumentWriter()));

                return subscriptions;
            }, (connectionId, subscriptions) =>
            {
                subscriptions.TryAdd(context.Op.Id,
                    new SubscriptionHandle(context.Op, stream, context.MessageWriter, new DocumentWriter()));

                return subscriptions;
            });
            _log.LogInformation($"Subscription: {context.Op.Id} started");
        }

        private async Task WriteOperationErrorsAsync(OperationMessageContext context,
            SubscriptionExecutionResult result)
        {
            var error = result.Errors.First();

            await context.MessageWriter.WriteMessageAsync(
                new OperationMessage
                {
                    Type = MessageTypes.GQL_ERROR,
                    Id = context.Op.Id,
                    Payload = JObject.FromObject(
                        new
                        {
                            message = error.Message,
                            locations = error.Locations
                        })
                });
        }

        private Task<SubscriptionExecutionResult> SubscribeAsync(GraphQuery query)
        {
            return _subscriptionExecuter.SubscribeAsync(new ExecutionOptions
            {
                Schema = _schema,
                OperationName = query.OperationName,
                Inputs = query.GetInputs(),
                Query = query.Query
            });
        }

        protected Task HandleConnectionInitAsync(OperationMessageContext context)
        {
            _log.LogInformation($"Acknowleding GraphQL connection: {context.ConnectionId}");
            return WriteConnectionAckAsync(context);
        }

        private Task WriteConnectionAckAsync(OperationMessageContext context)
        {
            return context.MessageWriter.WriteMessageAsync(new OperationMessage
            {
                Type = MessageTypes.GQL_CONNECTION_ACK
            });
        }
    }
}

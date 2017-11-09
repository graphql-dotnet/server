using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Subscription;
using GraphQL.Transports.AspNetCore.Requests;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionProtocolHandler<TSchema> : ISubscriptionProtocolHandler<TSchema> where TSchema : Schema
    {
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ILogger<SubscriptionProtocolHandler<TSchema>> _log;
        private readonly TSchema _schema;
        private readonly ISubscriptionExecuter _subscriptionExecuter;


        public SubscriptionProtocolHandler(
            TSchema schema,
            ISubscriptionExecuter subscriptionExecuter,
            IDocumentExecuter documentExecuter,
            ILogger<SubscriptionProtocolHandler<TSchema>> log)
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

        protected async Task HandleTerminateAsync(OperationMessageContext context)
        {
            if (Subscriptions.TryRemove(context.ConnectionId, out var subscriptions))
            {
                foreach (var subscription in subscriptions.Values)
                    await subscription.CloseAsync();

                subscriptions.Clear();
            }
        }

        protected async Task HandleStopAsync(OperationMessageContext context)
        {
            if (Subscriptions.TryGetValue(context.ConnectionId, out var subscriptions))
                if (subscriptions.TryRemove(context.Op.Id, out var subscriptionHandle))
                    await subscriptionHandle.CloseAsync();
        }

        protected async Task HandleStartAsync(OperationMessageContext context)
        {
            var query = context.Op.Payload.ToObject<GraphQuery>();
            var result = await SubscribeAsync(query).ConfigureAwait(false);

            await AddSubscription(context, result).ConfigureAwait(false);
            _log.LogInformation($"Subscription: {context.Op.Id} started");
        }

        public async Task AddSubscription(OperationMessageContext context, SubscriptionExecutionResult result)
        {
            if (result.Errors?.Any() == true)
            {
                await WriteOperationErrorsAsync(context, result.Errors).ConfigureAwait(false);
                return;
            }

            if (result.Streams == null || !result.Streams.Any())
            {
                await WriteOperationErrorsAsync(context, new[]
                {
                    new ExecutionError(
                        $"Could not resolve subsciption stream for {context.Op}")
                }).ConfigureAwait(false);
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
        }

        private async Task WriteOperationErrorsAsync(OperationMessageContext context,
            IEnumerable<ExecutionError> errors)
        {
            var error = errors?.FirstOrDefault();

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
                }).ConfigureAwait(false);
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

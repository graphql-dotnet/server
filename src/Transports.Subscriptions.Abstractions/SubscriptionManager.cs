using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions.Internal;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <inheritdoc />
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly IGraphQLExecuter _executer;

        private readonly ILogger<SubscriptionManager> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly ConcurrentDictionary<string, Subscription> _subscriptions =
            new ConcurrentDictionary<string, Subscription>();

        public SubscriptionManager(IGraphQLExecuter executer, ILoggerFactory loggerFactory)
        {
            _executer = executer;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<SubscriptionManager>();
        }

        public Subscription this[string id] => _subscriptions[id];

        public IEnumerator<Subscription> GetEnumerator()
        {
            return _subscriptions.Values.GetEnumerator();
        }

        /// <inheritdoc />
        public async Task SubscribeOrExecuteAsync(
            string id,
            OperationMessagePayload payload,
            MessageHandlingContext context)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (context == null) throw new ArgumentNullException(nameof(context));
            
            var subscription = await ExecuteAsync(id, payload, context);

            if (subscription == null)
                return;

            _subscriptions[id] = subscription;
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(string id)
        {
            if (_subscriptions.TryRemove(id, out var removed))
                return removed.UnsubscribeAsync();

            _logger.LogInformation("Subscription: {subcriptionId} unsubscribed", id);
            return Task.CompletedTask;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _subscriptions.Values.GetEnumerator();
        }

        private async Task<Subscription> ExecuteAsync(
            string id,
            OperationMessagePayload payload,
            MessageHandlingContext context)
        {
            var writer = context.Writer;
            _logger.LogDebug("Executing operation: {operationName} query: {query}",
                payload.OperationName,
                payload.Query);

            var result = await _executer.ExecuteAsync(
                payload.OperationName,
                payload.Query,
                payload.Variables, 
                context);

            if (result.Errors != null && result.Errors.Any())
            {
                _logger.LogError("Execution errors: {errors}", ResultHelper.GetErrorString(result));
                await writer.SendAsync(new OperationMessage
                {
                    Type = MessageType.GQL_ERROR,
                    Id = id,
                    Payload = JObject.FromObject(result)
                });

                return null;
            }

            // is sub
            if (result is SubscriptionExecutionResult subscriptionExecutionResult)
                using (_logger.BeginScope("Subscribing to: {subscriptionId}", id))
                {
                    if (subscriptionExecutionResult.Streams?.Values.SingleOrDefault() == null)
                    {
                        _logger.LogError("Cannot subscribe as no result stream available");
                        await writer.SendAsync(new OperationMessage
                        {
                            Type = MessageType.GQL_ERROR,
                            Id = id,
                            Payload = JObject.FromObject(result)
                        });

                        return null;
                    }

                    _logger.LogInformation("Creating subscription");
                    return new Subscription(
                        id,
                        payload,
                        subscriptionExecutionResult,
                        writer,
                        sub => _subscriptions.TryRemove(id, out _),
                        _loggerFactory.CreateLogger<Subscription>());
                }

            //is query or mutation
            await writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_DATA,
                Id = id,
                Payload = JObject.FromObject(result)
            });

            await writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_COMPLETE,
                Id = id
            });

            return null;
        }
    }
}
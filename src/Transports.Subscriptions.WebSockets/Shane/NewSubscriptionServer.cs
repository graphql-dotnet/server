#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Transport;
using Microsoft.Extensions.DependencyInjection;
using MessageType = GraphQL.Server.Transports.Subscriptions.Abstractions.MessageTypeGraphQLTransportWs;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane
{
    public class NewSubscriptionServer : OperationMessageServer
    {
        /// <summary>
        /// Returns the <see cref="IGraphQLExecuter"/> used to execute requests.
        /// </summary>
        protected IGraphQLExecuter GraphQLExecuter { get; }

        /// <summary>
        /// Returns the <see cref="IServiceScopeFactory"/> used to create a service scope for request execution.
        /// </summary>
        protected IServiceScopeFactory ServiceScopeFactory { get; }

        /// <summary>
        /// Returns the user context used to execute requests.
        /// </summary>
        protected IDictionary<string, object?> UserContext { get; }

        /// <summary>
        /// Returns the <see cref="IGraphQLSerializer"/> used to deserialize <see cref="OperationMessage"/> payloads.
        /// </summary>
        protected IGraphQLSerializer Serializer { get; }

        /// <summary>
        /// Initailizes a new instance with the specified parameters.
        /// </summary>
        /// <param name="sendStream">The WebSockets stream used to send data packets or close the connection.</param>
        /// <param name="connectionInitWaitTimeout">The amount of time to wait for a <see cref="MessageType.GQL_CONNECTION_INIT"/> message before terminating the connection. <see cref="Timeout.InfiniteTimeSpan"/> can be used to disable the timeout.</param>
        /// <param name="keepAliveTimeout">The periodic interval to send <see cref="MessageType.GQL_PING"/> messages after sending a <see cref="MessageType.GQL_CONNECTION_ACK"/>. <see cref="Timeout.InfiniteTimeSpan"/> can be used to disable the keep-alive signal.</param>
        /// <param name="executer">The <see cref="IGraphQLExecuter"/> to use to execute GraphQL requests.</param>
        /// <param name="serializer">The <see cref="IGraphQLSerializer"/> to use to deserialize payloads stored within <see cref="OperationMessage.Payload"/>.</param>
        /// <param name="serviceScopeFactory">A <see cref="IServiceScopeFactory"/> to create service scopes for execution of GraphQL requests.</param>
        /// <param name="userContext">The user context to pass to the <see cref="IGraphQLExecuter"/>.</param>
        public NewSubscriptionServer(
            IOperationMessageSendStream sendStream,
            TimeSpan connectionInitWaitTimeout,
            TimeSpan keepAliveTimeout,
            IGraphQLExecuter executer,
            IGraphQLSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IDictionary<string, object?> userContext)
            : base(sendStream, connectionInitWaitTimeout, keepAliveTimeout)
        {
            GraphQLExecuter = executer ?? throw new ArgumentNullException(nameof(executer));
            ServiceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            UserContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public override async Task OnMessageReceivedAsync(OperationMessage message)
        {
            if (message.Type == MessageType.GQL_PING)
            {
                await OnPing(message);
            }
            else if (message.Type == MessageType.GQL_CONNECTION_INIT)
            {
                if (!TryInitialize())
                {
                    await ErrorTooManyInitializationRequests();
                }
                else
                {
                    await OnConnectionInit(message);
                }
                return;
            }
            if (!Initialized)
            {
                await ErrorNotInitialized();
                return;
            }
            switch (message.Type)
            {
                case MessageType.GQL_SUBSCRIBE:
                    await OnSubscribe(message);
                    break;
                case MessageType.GQL_COMPLETE:
                    await OnComplete(message);
                    break;
                default:
                    await ErrorUnrecognizedMessage();
                    break;
            }
        }

        private static readonly OperationMessage _pongMessage = new() { Type = MessageType.GQL_PONG };
        /// <summary>
        /// Executes when a ping message is received.
        /// </summary>
        protected virtual Task OnPing(OperationMessage message)
            => Client.SendMessageAsync(_pongMessage);

        private static readonly OperationMessage _keepAliveMessage = new() { Type = MessageType.GQL_PING };
        /// <inheritdoc/>
        protected override Task OnSendKeepAlive()
            => Client.SendMessageAsync(_keepAliveMessage);

        private static readonly OperationMessage _connectionAckMessage = new() { Type = MessageType.GQL_CONNECTION_ACK };
        /// <inheritdoc/>
        protected override Task OnConnectionAcknowledge(OperationMessage message)
            => Client.SendMessageAsync(_connectionAckMessage);

        /// <summary>
        /// Executes when a request is received to start a subscription.
        /// </summary>
        protected virtual Task OnSubscribe(OperationMessage message)
            => base.Subscribe(message, false);

        /// <summary>
        /// Executes when a request is received to stop a subscription.
        /// </summary>
        protected virtual Task OnComplete(OperationMessage message)
            => message.Id != null ? Unsubscribe(message.Id) : Task.CompletedTask;

        /// <inheritdoc/>
        protected override async Task SendErrorResult(string id, ExecutionResult result)
        {
            if (Subscriptions.TryRemove(id))
            {
                await Client.SendMessageAsync(new OperationMessage
                {
                    Id = id,
                    Type = MessageType.GQL_ERROR,
                    Payload = result,
                });
            }
        }

        /// <inheritdoc/>
        protected override async Task SendData(string id, ExecutionResult result)
        {
            if (Subscriptions.Contains(id))
            {
                await Client.SendMessageAsync(new OperationMessage
                {
                    Id = id,
                    Type = MessageType.GQL_NEXT,
                    Payload = result,
                });
            }
        }

        /// <inheritdoc/>
        protected override async Task SendCompleted(string id)
        {
            if (Subscriptions.TryRemove(id))
            {
                await Client.SendMessageAsync(new OperationMessage
                {
                    Id = id,
                    Type = MessageType.GQL_COMPLETE,
                });
            }
        }

        /// <inheritdoc/>
        protected override async Task<ExecutionResult> ExecuteRequest(OperationMessage message)
        {
            var request = Serializer.ReadNode<GraphQLRequest>(message.Payload);
            using var scope = ServiceScopeFactory.CreateScope();
            return await GraphQLExecuter.ExecuteAsync(request, UserContext, scope.ServiceProvider, CancellationToken);
        }
    }
}

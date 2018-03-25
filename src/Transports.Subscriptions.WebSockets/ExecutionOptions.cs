using System.Collections.Generic;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;

namespace GraphQL.Server.Transports.WebSockets
{
    /// <summary>
    ///     GraphQL execution options for TSchema
    /// </summary>
    /// <typeparam name="TSchema"></typeparam>
    public class ExecutionOptions<TSchema> : ExecutionOptions where TSchema : ISchema
    {
        public ExecutionOptions()
        {
            MessageListeners = new List<IOperationMessageListener>();
        }

        public List<IOperationMessageListener> MessageListeners { get; set; }

        public IGraphQLExecuterFactory<TSchema> ExecuterFactory { get; set; }
    }
}
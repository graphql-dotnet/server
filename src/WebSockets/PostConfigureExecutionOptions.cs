using System.Collections.Generic;
using GraphQL.Execution;
using GraphQL.Types;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    /// <summary>
    ///     Configure <see cref="IDocumentExecutionListener"/> from container
    /// </summary>
    /// <typeparam name="TSchema"></typeparam>
    public class PostConfigureExecutionOptions<TSchema> : IPostConfigureOptions<ExecutionOptions<TSchema>>
        where TSchema : ISchema
    {
        private readonly IEnumerable<IDocumentExecutionListener> _listeners;

        public PostConfigureExecutionOptions(
            IEnumerable<IDocumentExecutionListener> listeners)
        {
            _listeners = listeners;
        }

        public void PostConfigure(string name, ExecutionOptions<TSchema> options)
        {
            // do not override
            if (options.Listeners == null)
                options.Listeners = _listeners;
        }
    }
}
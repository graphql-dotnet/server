using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    /// <summary>
    ///     Executer that can be configured with <see cref="ExecutionOptions"/>
    /// </summary>
    /// <typeparam name="TSchema"></typeparam>
    public class ConfigurableExecuter<TSchema> : DefaultSchemaExecuter<TSchema> where TSchema : ISchema
    {
        private readonly ExecutionOptions<TSchema> _options;

        public ConfigurableExecuter(
            IDocumentExecuter documentExecuter,
            TSchema schema,
            IOptions<ExecutionOptions<TSchema>> options) : base(documentExecuter, schema)
        {
            _options = options.Value;
        }

        protected override ExecutionOptions GetOptions(string operationName, string query, JObject variables,
            MessageHandlingContext context)
        {
            var options = base.GetOptions(operationName, query, variables, context);

            if (_options.Listeners != null)
                foreach (var listener in _options.Listeners)
                    options.Listeners.Add(listener);

            options.EnableMetrics = _options.EnableMetrics;
            options.ComplexityConfiguration = _options.ComplexityConfiguration;
            options.ExposeExceptions = _options.ExposeExceptions;
            options.FieldMiddleware = _options.FieldMiddleware;
            options.FieldNameConverter = _options.FieldNameConverter;
            options.SetFieldMiddleware = _options.SetFieldMiddleware;
            options.ValidationRules = _options.ValidationRules;

            // add customer UserContext as property to MessageHandlingContext
            context.Properties["UserContext"] = _options.UserContext;

            return options;
        }
    }
}
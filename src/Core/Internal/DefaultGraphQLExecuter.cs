using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Internal
{
    public class DefaultGraphQLExecuter<TSchema> : IGraphQLExecuter<TSchema>
        where TSchema : ISchema
    {
        public TSchema Schema { get; }

        private readonly IDocumentExecuter _documentExecuter;
        private readonly GraphQLOptions _options;
        private readonly IEnumerable<IDocumentExecutionListener> _listeners;
        private readonly IEnumerable<IValidationRule> _validationRules;

        public DefaultGraphQLExecuter(
            TSchema schema,
            IDocumentExecuter documentExecuter,
            IOptions<GraphQLOptions> options,
            IEnumerable<IDocumentExecutionListener> listeners,
            IEnumerable<IValidationRule> validationRules)
        {
            Schema = schema;

            _documentExecuter = documentExecuter;
            _options = options.Value;
            _listeners = listeners;
            _validationRules = validationRules;
        }

        public virtual async Task<ExecutionResult> ExecuteAsync(string operationName, string query, Inputs variables, IDictionary<string, object> context, IServiceProvider requestServices, CancellationToken cancellationToken = default)
        {
            var start = DateTime.UtcNow;

            var options = GetOptions(operationName, query, variables, context, requestServices, cancellationToken);
            var result = await _documentExecuter.ExecuteAsync(options);

            if (options.EnableMetrics)
            {
                result.EnrichWithApolloTracing(start);
            }

            return result;
        }

        protected virtual ExecutionOptions GetOptions(string operationName, string query, Inputs variables, IDictionary<string, object> context, IServiceProvider requestServices, CancellationToken cancellationToken)
        {
            var opts = new ExecutionOptions
            {
                Schema = Schema,
                OperationName = operationName,
                Query = query,
                Inputs = variables,
                UserContext = context,
                CancellationToken = cancellationToken,
                ComplexityConfiguration = _options.ComplexityConfiguration,
                EnableMetrics = _options.EnableMetrics,
                NameConverter = _options.NameConverter ?? CamelCaseNameConverter.Instance,
                UnhandledExceptionDelegate = _options.UnhandledExceptionDelegate,
                SchemaFilter = _options.SchemaFilter ?? new DefaultSchemaFilter(),
                MaxParallelExecutionCount = _options.MaxParallelExecutionCount,
                RequestServices = requestServices,
            };

            if (opts.EnableMetrics)
            {
                opts.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
            }

            foreach (var listener in _listeners)
            {
                opts.Listeners.Add(listener);
            }

            var customRules = _validationRules.ToArray();
            if (customRules.Length > 0)
            {
                // if not set then standard list of validation rules (DocumentValidator.CoreRules) will be used by DocumentValidator
                // else concatenate standard rules with custom ones preferring the standard to go first
                opts.ValidationRules = DocumentValidator.CoreRules
                    .Concat(customRules)
                    .ToList();
            }

            return opts;
        }
    }
}

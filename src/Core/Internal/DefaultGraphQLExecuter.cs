using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public virtual async Task<ExecutionResult> ExecuteAsync(string operationName, string query, Inputs variables, IDictionary<string, object> context, CancellationToken cancellationToken = default)
        {
            var start = DateTime.UtcNow;

            var options = GetOptions(operationName, query, variables, context, cancellationToken);
            var result = await _documentExecuter.ExecuteAsync(options);

            if (options.EnableMetrics)
            {
                result.EnrichWithApolloTracing(start);
            }

            return result;
        }

        protected virtual ExecutionOptions GetOptions(string operationName, string query, Inputs variables, IDictionary<string, object> context, CancellationToken cancellationToken)
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
                ExposeExceptions = _options.ExposeExceptions,
                FieldNameConverter = _options.FieldNameConverter ?? CamelCaseFieldNameConverter.Instance,
            };

            if (opts.EnableMetrics)
            {
                opts.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
            }

            foreach (var listener in _listeners)
            {
                opts.Listeners.Add(listener);
            }

            opts.ValidationRules = _validationRules
                .Concat(DocumentValidator.CoreRules())
                .ToList();

            return opts;
        }
    }
}
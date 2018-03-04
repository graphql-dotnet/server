using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using GraphQL;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class DefaultSchemaExecuter<TSchema> : IGraphQLExecuter where TSchema : ISchema
    {
        private readonly IDocumentExecuter _documentExecuter;
        private readonly TSchema _schema;

        public DefaultSchemaExecuter(IDocumentExecuter documentExecuter, TSchema schema)
        {
            _documentExecuter = documentExecuter;
            _schema = schema;
        }

        public virtual Task<ExecutionResult> ExecuteAsync(string operationName, string query, JObject variables)
        {
            var options = GetOptions(operationName, query, variables);

            return _documentExecuter.ExecuteAsync(options);
        }

        protected virtual ExecutionOptions GetOptions(string operationName, string query, JObject variables)
        {
            var options = new ExecutionOptions
            {
                Schema = _schema,
                OperationName = operationName,
                Query = query,
                Inputs = variables.ToInputs()
            };
            return options;
        }
    }
}
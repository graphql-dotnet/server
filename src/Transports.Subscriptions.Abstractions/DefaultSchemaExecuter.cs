using System.Threading.Tasks;
using GraphQL.Types;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class DefaultSchemaExecuter<TSchema> : IGraphQLExecuter where TSchema : ISchema
    {
        protected readonly TSchema Schema;
        private readonly IDocumentExecuter _documentExecuter;

        public DefaultSchemaExecuter(
            IDocumentExecuter documentExecuter,
            TSchema schema)
        {
            _documentExecuter = documentExecuter;
            Schema = schema;
        }

        public virtual Task<ExecutionResult> ExecuteAsync(string operationName, string query, JObject variables,
            MessageHandlingContext context)
        {
            var options = GetOptions(operationName, query, variables, context);
            return _documentExecuter.ExecuteAsync(options);
        }

        protected virtual ExecutionOptions GetOptions(string operationName, string query, JObject variables,
            MessageHandlingContext context)
        {
            var options = new ExecutionOptions
            {
                Schema = Schema,
                OperationName = operationName,
                Query = query,
                Inputs = variables.ToInputs(),
                UserContext = context
            };
            return options;
        }
    }
}
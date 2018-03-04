using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class DefaultGraphQLExecuter : IGraphQLExecuter
    {
        private readonly IDocumentExecuter _documentExecuter;

        public DefaultGraphQLExecuter(IDocumentExecuter documentExecuter)
        {
            _documentExecuter = documentExecuter;
        }

        public virtual Task<ExecutionResult> ExecuteAsync(string operationName, string query, dynamic variables)
        {
            var options = GetOptions(operationName, query, variables);

            return _documentExecuter.ExecuteAsync(options);
        }

        protected virtual ExecutionOptions GetOptions(string operationName, string query, dynamic variables)
        {
            var options = new ExecutionOptions()
            {
                OperationName = operationName,
                Query = query,
                Inputs = new Inputs(variables)
            };
            return options;
        }
    }
}
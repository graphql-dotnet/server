using System.Linq;
using GraphQL.Execution;
using GraphQL.Language.AST;
using GraphQL.Server.Transports.WebSockets.Abstractions;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionDeterminator : ISubscriptionDeterminator
    {
        private readonly IDocumentBuilder _documentBuilder;

        public SubscriptionDeterminator(): this(new GraphQLDocumentBuilder())
        {
        }

        public SubscriptionDeterminator(IDocumentBuilder documentBuilder)
        {
            _documentBuilder = documentBuilder;
        }

        public bool IsSubscription(ExecutionOptions config)
        {
            ValidateOptions(config);

            config.Document = config.Document ?? _documentBuilder.Build(config.Query);

            return GetOperation(config.OperationName, config.Document).OperationType == OperationType.Subscription;
        }

        private void ValidateOptions(ExecutionOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Query))
            {
                throw new ExecutionError("A query is required.");
            }
        }

        protected virtual Operation GetOperation(string operationName, Document document)
        {
            var operation = !string.IsNullOrWhiteSpace(operationName)
                ? document.Operations.WithName(operationName)
                : document.Operations.FirstOrDefault();

            return operation;
        }
    }
}

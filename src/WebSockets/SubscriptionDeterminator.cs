using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using GraphQLParser.AST;
using OperationType = GraphQL.Language.AST.OperationType;

namespace GraphQL.Server.Transports.WebSockets
{
    public class SubscriptionDeterminator
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

            config.Schema.FieldNameConverter = config.FieldNameConverter;

            ValidateOptions(config);

            if (!config.Schema.Initialized)
            {
                config.Schema.Initialize();
            }

            var document = config.Document ?? _documentBuilder.Build(config.Query);

            return GetOperation(config.OperationName, document).OperationType == OperationType.Subscription;
        }

        private void ValidateOptions(ExecutionOptions options)
        {
            if (options.Schema == null)
            {
                throw new ExecutionError("A schema is required.");
            }

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

using GraphQL.Types;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests;

internal class SchemaDocumentExecuter : IDocumentExecuter
{
    private readonly ISchema _schema;
    public SchemaDocumentExecuter(ISchema schema)
    {
        _schema = schema;
    }

    public Task<ExecutionResult> ExecuteAsync(ExecutionOptions options)
    {
        options.Schema = _schema;
        return new DocumentExecuter().ExecuteAsync(options);
    }
}

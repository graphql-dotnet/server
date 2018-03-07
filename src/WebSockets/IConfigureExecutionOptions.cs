using GraphQL.Types;

namespace GraphQL.Server.Transports.WebSockets
{
    public interface IConfigureExecutionOptions<TSchema> where TSchema:ISchema
    {
        void Configure(TSchema schema, ExecutionOptions options);
    }
}
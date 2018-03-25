using GraphQL.Types;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public class UseConfigurableExecuterFactory<TSchema> : IConfigureOptions<ExecutionOptions<TSchema>> where TSchema : ISchema
    {
        private readonly IGraphQLExecuterFactory<TSchema> _executerFactory;

        public UseConfigurableExecuterFactory(IGraphQLExecuterFactory<TSchema> executerFactory)
        {
            _executerFactory = executerFactory;
        }

        public void Configure(ExecutionOptions<TSchema> options)
        {
            options.ExecuterFactory = _executerFactory;
        }
    }
}
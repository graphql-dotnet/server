using System.Collections.Generic;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.WebSockets
{
    public class ConfigurableExecuter<TSchema> : DefaultSchemaExecuter<TSchema> where TSchema : ISchema
    {
        private readonly IEnumerable<IConfigureExecutionOptions<TSchema>> _configureExecutionOptions;

        public ConfigurableExecuter(
            IDocumentExecuter documentExecuter, 
            TSchema schema,
            IEnumerable<IConfigureExecutionOptions<TSchema>> configureExecutionOptions) : base(documentExecuter, schema)
        {
            _configureExecutionOptions = configureExecutionOptions;
        }

        protected override ExecutionOptions GetOptions(string operationName, string query, JObject variables)
        {
            var options = base.GetOptions(operationName, query, variables);

            foreach ( var configureExecutionOption in _configureExecutionOptions)
            {
                configureExecutionOption.Configure(Schema, options);
            }

            return options;
        }
    }
}
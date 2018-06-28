using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.Validation;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public class ExecutionOptionsFactory : IExecutionOptionsFactory
    {
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IEnumerable<IDocumentExecutionListener> _documentListeners;

        public ExecutionOptionsFactory(
            IEnumerable<IValidationRule> validationRules,
            IEnumerable<IDocumentExecutionListener> documentListeners
            )
        {
            _validationRules = validationRules;
            _documentListeners = documentListeners;
        }

        public virtual async Task<ExecutionOptions> CreateExecutionOptionsAsync()
        {
            var opts = new ExecutionOptions
            {
                EnableMetrics = true,
                SetFieldMiddleware = true,
                ValidationRules = _validationRules,
            };

            if (_documentListeners != null)
            {
                foreach (var listener in _documentListeners)
                {
                    opts.Listeners.Add(listener);
                }
            }

            return await Task.FromResult(opts);
        }
    }
}

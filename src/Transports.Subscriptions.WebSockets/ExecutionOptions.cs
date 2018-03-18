using System.Collections.Generic;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace GraphQL.Server.Transports.WebSockets
{
    /// <summary>
    ///     GraphQL execution options for TSchema
    /// </summary>
    /// <typeparam name="TSchema"></typeparam>
    public class ExecutionOptions<TSchema> where TSchema : ISchema
    {
        public IEnumerable<IDocumentExecutionListener> Listeners { get; set; }

        public IEnumerable<IValidationRule> ValidationRules { get; set; }

        public object UserContext { get; set; }

        public IFieldMiddlewareBuilder FieldMiddleware { get; set; } =
            new FieldMiddlewareBuilder();

        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        public IFieldNameConverter FieldNameConverter { get; set; } =
            new CamelCaseFieldNameConverter();

        public bool ExposeExceptions { get; set; }

        public bool EnableMetrics { get; set; } = true;

        public bool SetFieldMiddleware { get; set; } = true;
    }
}
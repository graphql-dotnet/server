using System;
using System.Collections.Generic;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public Func<HttpContext, object> BuildUserContext { get; set; }

        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        public bool EnableMetrics { get; set; } = true;

        public bool ExposeExceptions { get; set; }

        public bool SetFieldMiddleware { get; set; } = true;

        public IList<IValidationRule> ValidationRules { get; } = new List<IValidationRule>();
    }
}

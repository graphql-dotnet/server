using System;
using System.Collections.Generic;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public Func<HttpContext, object> BuildUserContext { get; set; }

        public bool ExposeExceptions { get; set; }

        public IList<IValidationRule> ValidationRules { get; } = new List<IValidationRule>();
    }
}

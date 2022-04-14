using System.Security.Claims;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Server.Authorization.AspNetCore.Tests
{
    public class ValidationTestConfig
    {
        public string Query { get; set; }

        public ISchema Schema { get; set; }

        public List<IValidationRule> Rules { get; set; } = new List<IValidationRule>();

        public ClaimsPrincipal User { get; set; }

        public Inputs Inputs { get; set; }

        public Action<IValidationResult> ValidateResult = _ => { };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace GraphQL.Server.Authorization.AspNetCore.Tests
{
    public class ValidationTestConfig
    {
        private readonly List<IValidationRule> _rules = new List<IValidationRule>();

        public string Query { get; set; }
        public ISchema Schema { get; set; }
        public IEnumerable<IValidationRule> Rules => _rules;
        public ClaimsPrincipal User { get; set; }
        public Inputs Inputs { get; set; }

        public void Rule(params IValidationRule[] rules)
        {
            _rules.AddRange(rules);
        }
    }
    public class GraphQLUserContext : Dictionary<string, object>
    {
        public ClaimsPrincipal User { get; set; }
    }
    public class ValidationTestBase
    {
        protected HttpContext HttpContext { get; private set; }

        protected AuthorizationValidationRule Rule { get; private set; }

        protected void ConfigureAuthorizationOptions(Action<AuthorizationOptions> setupOptions)
        {
            var (authorizationService, httpContextAccessor) = BuildServices(setupOptions);
            HttpContext = httpContextAccessor.HttpContext;
            Rule = new AuthorizationValidationRule(authorizationService, httpContextAccessor);
        }

        protected void ShouldPassRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            config.Rule(Rule);
            configure(config);

            config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

            config.Schema.Initialize();

            var result = Validate(config);

            var message = "";
            if (result.Errors?.Any() == true)
            {
                message = string.Join(", ", result.Errors.Select(x => x.Message));
            }
            result.IsValid.ShouldBeTrue(message);
        }

        protected void ShouldFailRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            config.Rule(Rule);
            configure(config);

            config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

            config.Schema.Initialize();

            var result = Validate(config);

            result.IsValid.ShouldBeFalse("Expected validation errors though there were none.");
        }

        private (IAuthorizationService, IHttpContextAccessor) BuildServices(Action<AuthorizationOptions> setupOptions)
        {
            var services = new ServiceCollection();
            services.AddAuthorization(setupOptions);
            services.AddLogging();
            services.AddOptions();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            var serviceProvider = services.BuildServiceProvider();
            var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
            httpContextAccessor.HttpContext = new DefaultHttpContext();
            return (authorizationService, httpContextAccessor);
        }

        private IValidationResult Validate(ValidationTestConfig config)
        {
            this.HttpContext.User = config.User;
            var userContext = new GraphQLUserContext { User = config.User };
            var documentBuilder = new GraphQLDocumentBuilder();
            var document = documentBuilder.Build(config.Query);
            var validator = new DocumentValidator();
            return validator.Validate(config.Query, config.Schema, document, config.Rules, userContext, config.Inputs);
        }

        protected ClaimsPrincipal CreatePrincipal(string authenticationType = null, IDictionary<string, string> claims = null)
        {
            var claimsList = new List<Claim>();

            claims?.Apply(c =>
            {
                claimsList.Add(new Claim(c.Key, c.Value));
            });

            return new ClaimsPrincipal(new ClaimsIdentity(claimsList, authenticationType));
        }
    }
}

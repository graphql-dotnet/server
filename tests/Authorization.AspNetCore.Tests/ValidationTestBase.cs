using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GraphQL.Execution;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace GraphQL.Server.Authorization.AspNetCore.Tests
{
    public class ValidationTestBase : IDisposable
    {
        protected ServiceProvider ServiceProvider { get; private set; }

        protected HttpContext HttpContext { get; private set; }

        protected AuthorizationValidationRule Rule { get; private set; }

        protected void ConfigureAuthorizationOptions(Action<AuthorizationOptions> setupOptions)
        {
            var (authorizationService, httpContextAccessor) = BuildServices(setupOptions);
            HttpContext = httpContextAccessor.HttpContext;
            Rule = new AuthorizationValidationRule(authorizationService, new DefaultClaimsPrincipalAccessor(httpContextAccessor), new DefaultAuthorizationErrorMessageBuilder());
        }

        protected void ShouldPassRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            config.Rules.Add(Rule);
            configure(config);

            config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

            config.Schema.Initialize();

            var result = Validate(config);

            string message = "";
            if (result.Errors?.Any() == true)
            {
                message = string.Join(", ", result.Errors.Select(x => x.Message));
            }
            result.IsValid.ShouldBeTrue(message);
            config.ValidateResult(result);
        }

        protected void ShouldFailRule(Action<ValidationTestConfig> configure)
        {
            var config = new ValidationTestConfig();
            config.Rules.Add(Rule);
            configure(config);

            config.Rules.Any().ShouldBeTrue("Must provide at least one rule to validate against.");

            config.Schema.Initialize();

            var result = Validate(config);

            result.IsValid.ShouldBeFalse("Expected validation errors though there were none.");
            config.ValidateResult(result);
        }

        private (IAuthorizationService, IHttpContextAccessor) BuildServices(Action<AuthorizationOptions> setupOptions)
        {
            if (ServiceProvider != null)
                throw new InvalidOperationException("BuildServices has been already called");

            var services = new ServiceCollection()
                           .AddAuthorization(setupOptions)
                           .AddLogging()
                           .AddOptions()
                           .AddHttpContextAccessor();

            ServiceProvider = services.BuildServiceProvider();

            var authorizationService = ServiceProvider.GetRequiredService<IAuthorizationService>();
            var httpContextAccessor = ServiceProvider.GetRequiredService<IHttpContextAccessor>();

            httpContextAccessor.HttpContext = new DefaultHttpContext();
            return (authorizationService, httpContextAccessor);
        }

        private IValidationResult Validate(ValidationTestConfig config)
        {
            HttpContext.User = config.User;
            var documentBuilder = new GraphQLDocumentBuilder();
            var document = documentBuilder.Build(config.Query);
            var validator = new DocumentValidator();
            return validator.ValidateAsync(new ValidationOptions
            {
                Schema = config.Schema,
                Document = document,
                Operation = document.Definitions.OfType<GraphQLOperationDefinition>().First(),
                Rules = config.Rules,
                Variables = config.Inputs
            }).GetAwaiter().GetResult().validationResult;
        }

        protected ClaimsPrincipal CreatePrincipal(string authenticationType = null, IDictionary<string, string> claims = null)
        {
            var claimsList = new List<Claim>();

            if (claims != null)
            {
                foreach (var c in claims)
                    claimsList.Add(new Claim(c.Key, c.Value));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claimsList, authenticationType));
        }

        public void Dispose()
        {
            ServiceProvider.Dispose();
        }
    }
}

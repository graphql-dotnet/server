using System;
using GraphQL.Server.Ui.Altair;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/> to add <see cref="AltairMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class AltairEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Add the Altair middleware to the HTTP request pipeline
        /// </summary>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static AltairEndpointConventionBuilder MapGraphQLAltair(this IEndpointRouteBuilder endpoints, string pattern = "ui/altair")
            => endpoints.MapGraphQLAltair(new AltairOptions(), pattern);

        /// <summary>
        /// Add the Altair middleware to the HTTP request pipeline
        /// </summary>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="options">Options to customize <see cref="AltairMiddleware"/>. If not set, then the default values will be used.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static AltairEndpointConventionBuilder MapGraphQLAltair(this IEndpointRouteBuilder endpoints, AltairOptions options, string pattern = "ui/altair")
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            var requestDelegate = endpoints.CreateApplicationBuilder().UseMiddleware<AltairMiddleware>(options ?? new AltairOptions()).Build();
            return new AltairEndpointConventionBuilder(endpoints.Map(pattern, requestDelegate).WithDisplayName("GraphQL Altair"));
        }
    }

    /// <summary>
    /// Builds conventions that will be used for customization of Microsoft.AspNetCore.Builder.EndpointBuilder instances.
    /// Special convention builder that allows you to write specific extension methods for ASP.NET Core routing subsystem.
    /// </summary>
    public class AltairEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        internal AltairEndpointConventionBuilder(IEndpointConventionBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention) => _builder.Add(convention);
    }
}

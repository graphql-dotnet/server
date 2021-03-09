using System;
using GraphQL.Server.Ui.Voyager;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/> to add <see cref="VoyagerMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class VoyagerEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Add the Voyager middleware to the HTTP request pipeline
        /// </summary>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static VoyagerEndpointConventionBuilder MapGraphQLVoyager(this IEndpointRouteBuilder endpoints, string pattern = "ui/voyager")
            => endpoints.MapGraphQLVoyager(new VoyagerOptions(), pattern);

        /// <summary>
        /// Add the Voyager middleware to the HTTP request pipeline
        /// </summary>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="options">Options to customize <see cref="VoyagerMiddleware"/>. If not set, then the default values will be used.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static VoyagerEndpointConventionBuilder MapGraphQLVoyager(this IEndpointRouteBuilder endpoints, VoyagerOptions options, string pattern = "ui/voyager")
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            var requestDelegate = endpoints.CreateApplicationBuilder().UseMiddleware<VoyagerMiddleware>(options ?? new VoyagerOptions()).Build();
            return new VoyagerEndpointConventionBuilder(endpoints.Map(pattern, requestDelegate).WithDisplayName("GraphQL Voyager"));
        }
    }

    /// <summary>
    /// Builds conventions that will be used for customization of Microsoft.AspNetCore.Builder.EndpointBuilder instances.
    /// Special convention builder that allows you to write specific extension methods for ASP.NET Core routing subsystem.
    /// </summary>
    public class VoyagerEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        internal VoyagerEndpointConventionBuilder(IEndpointConventionBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention) => _builder.Add(convention);
    }
}

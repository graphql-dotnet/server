using System;
using GraphQL.Server.Ui.GraphiQL;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/> to add <see cref="GraphiQLMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class GraphiQLEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Add the GraphiQL middleware to the HTTP request pipeline
        /// </summary>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static GraphiQLEndpointConventionBuilder MapGraphQLGraphiQL(this IEndpointRouteBuilder endpoints, string pattern = "ui/graphiql")
            => endpoints.MapGraphQLGraphiQL(new GraphiQLOptions(), pattern);

        /// <summary>
        /// Add the GraphiQL middleware to the HTTP request pipeline
        /// </summary>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="options">Options to customize <see cref="GraphiQLMiddleware"/>. If not set, then the default values will be used.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static GraphiQLEndpointConventionBuilder MapGraphQLGraphiQL(this IEndpointRouteBuilder endpoints, GraphiQLOptions options, string pattern = "ui/graphiql")
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            var requestDelegate = endpoints.CreateApplicationBuilder().UseMiddleware<GraphiQLMiddleware>(options ?? new GraphiQLOptions()).Build();
            return new GraphiQLEndpointConventionBuilder(endpoints.Map(pattern, requestDelegate).WithDisplayName("GraphiQL"));
        }
    }

    /// <summary>
    /// Builds conventions that will be used for customization of Microsoft.AspNetCore.Builder.EndpointBuilder instances.
    /// Special convention builder that allows you to write specific extension methods for ASP.NET Core routing subsystem.
    /// </summary>
    public class GraphiQLEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        internal GraphiQLEndpointConventionBuilder(IEndpointConventionBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention) => _builder.Add(convention);
    }
}

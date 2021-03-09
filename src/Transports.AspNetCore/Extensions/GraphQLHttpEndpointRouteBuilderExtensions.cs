using System;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/> to add <see cref="GraphQLHttpMiddleware{TSchema}"/>
    /// or its descendants in the HTTP request pipeline.
    /// </summary>
    public static class GraphQLHttpEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Add the GraphQL middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static GraphQLHttpEndpointConventionBuilder MapGraphQL<TSchema>(this IEndpointRouteBuilder endpoints, string pattern = "graphql")
             where TSchema : ISchema
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            var requestDelegate = endpoints.CreateApplicationBuilder().UseMiddleware<GraphQLHttpMiddleware<TSchema>>().Build();
            return new GraphQLHttpEndpointConventionBuilder(endpoints.Map(pattern, requestDelegate).WithDisplayName("GraphQL"));
        }

        /// <summary>
        /// Add the GraphQL middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <typeparam name="TMiddleware">Custom middleware inherited from <see cref="GraphQLHttpMiddleware{TSchema}"/></typeparam>
        /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
        /// <param name="pattern">The route pattern.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static GraphQLHttpEndpointConventionBuilder MapGraphQL<TSchema, TMiddleware>(this IEndpointRouteBuilder endpoints, string pattern = "graphql")
             where TSchema : ISchema
             where TMiddleware : GraphQLHttpMiddleware<TSchema>
        {
            if (endpoints == null)
                throw new ArgumentNullException(nameof(endpoints));

            var requestDelegate = endpoints.CreateApplicationBuilder().UseMiddleware<TMiddleware>().Build();
            return new GraphQLHttpEndpointConventionBuilder(endpoints.Map(pattern, requestDelegate).WithDisplayName("GraphQL"));
        }
    }

    /// <summary>
    /// Builds conventions that will be used for customization of Microsoft.AspNetCore.Builder.EndpointBuilder instances.
    /// Special convention builder that allows you to write specific extension methods for ASP.NET Core routing subsystem.
    /// </summary>
    public class GraphQLHttpEndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        internal GraphQLHttpEndpointConventionBuilder(IEndpointConventionBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention) => _builder.Add(convention);
    }
}

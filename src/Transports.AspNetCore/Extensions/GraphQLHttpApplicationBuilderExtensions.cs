using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="GraphQLHttpMiddleware{TSchema}"/>
/// or its descendants in the HTTP request pipeline.
/// </summary>
public static class GraphQLHttpApplicationBuilderExtensions
{
    /// <summary>
    /// Add the GraphQL middleware to the HTTP request pipeline.
    /// <br/><br/>
    /// Uses the GraphQL schema registered as <see cref="ISchema"/> within the dependency injection
    /// framework to execute the query.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
    /// <param name="configureMiddleware">A delegate to configure the middleware</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL(this IApplicationBuilder builder, string path = "/graphql", Action<GraphQLHttpMiddlewareOptions>? configureMiddleware = null)
        => builder.UseGraphQL<ISchema>(path, configureMiddleware);

    /// <summary>
    /// Add the GraphQL middleware to the HTTP request pipeline.
    /// <br/><br/>
    /// Uses the GraphQL schema registered as <see cref="ISchema"/> within the dependency injection
    /// framework to execute the query.
    /// </summary>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint</param>
    /// <param name="configureMiddleware">A delegate to configure the middleware</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL(this IApplicationBuilder builder, PathString path, Action<GraphQLHttpMiddlewareOptions>? configureMiddleware = null)
        => builder.UseGraphQL<ISchema>(path, configureMiddleware);

    /// <summary>
    /// Add the GraphQL middleware to the HTTP request pipeline for the specified schema.
    /// </summary>
    /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
    /// <param name="configureMiddleware">A delegate to configure the middleware</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, string path = "/graphql", Action<GraphQLHttpMiddlewareOptions>? configureMiddleware = null)
        where TSchema : ISchema
        => builder.UseGraphQL<TSchema>(new PathString(path), configureMiddleware);

    /// <summary>
    /// Add the GraphQL middleware to the HTTP request pipeline for the specified schema.
    /// </summary>
    /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint</param>
    /// <param name="configureMiddleware">A delegate to configure the middleware</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, PathString path, Action<GraphQLHttpMiddlewareOptions>? configureMiddleware = null)
        where TSchema : ISchema
    {
        var opts = new GraphQLHttpMiddlewareOptions();
        configureMiddleware?.Invoke(opts);
        return builder.UseWhen(
            context => context.Request.Path.Equals(path),
            b => b.UseMiddleware<GraphQLHttpMiddleware<TSchema>>(opts));
    }

    /// <summary>
    /// Add the GraphQL custom middleware to the HTTP request pipeline for the specified schema.
    /// </summary>
    /// <typeparam name="TMiddleware">Custom middleware inherited from <see cref="GraphQLHttpMiddleware{TSchema}"/></typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
    /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        this IApplicationBuilder builder, string path = "/graphql", params object[] args)
        where TMiddleware : GraphQLHttpMiddleware
        => builder.UseGraphQL<TMiddleware>(new PathString(path), args);

    /// <summary>
    /// Add the GraphQL custom middleware to the HTTP request pipeline for the specified schema.
    /// </summary>
    /// <typeparam name="TMiddleware">Custom middleware inherited from <see cref="GraphQLHttpMiddleware{TSchema}"/></typeparam>
    /// <param name="builder">The application builder</param>
    /// <param name="path">The path to the GraphQL endpoint</param>
    /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseGraphQL<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] TMiddleware>(
        this IApplicationBuilder builder, PathString path, params object[] args)
        where TMiddleware : GraphQLHttpMiddleware
    {
        return builder.UseWhen(
            context => context.Request.Path.Equals(path),
            b => b.UseMiddleware<TMiddleware>(args));
    }

    /// <summary>
    /// Enables WebSockets and rejects WebSocket requests whose Origin header does not match
    /// the request's scheme and Host header, including the port.
    /// </summary>
    /// <remarks>
    /// Requests without an Origin header are allowed, matching the behavior of
    /// <c>WebSocketOptions.AllowedOrigins</c>. Register this before GraphQL middleware
    /// or endpoint execution. If using forwarded headers, configure them before this middleware.
    /// </remarks>
    /// <param name="builder">The application builder</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseWebSocketsWithSameOriginCheck(this IApplicationBuilder builder)
        => builder.UseWebSocketsWithSameOriginCheck(new WebSocketOptions());

    /// <summary>
    /// Enables WebSockets with the specified options and rejects WebSocket requests whose
    /// Origin header does not match the request's scheme and Host header, including the port.
    /// </summary>
    /// <remarks>
    /// Requests without an Origin header are allowed. Any restrictions specified by
    /// <c>WebSocketOptions.AllowedOrigins</c> apply in addition to the same-origin check.
    /// Register this before GraphQL middleware or endpoint execution. If using forwarded headers,
    /// configure them before this middleware.
    /// </remarks>
    /// <param name="builder">The application builder</param>
    /// <param name="options">The WebSocket middleware options</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static IApplicationBuilder UseWebSocketsWithSameOriginCheck(this IApplicationBuilder builder, WebSocketOptions options)
    {
        builder.UseWebSockets(options);
        return builder.Use(static next => context =>
        {
            if (context.WebSockets.IsWebSocketRequest &&
                context.Request.Headers.TryGetValue("Origin", out var origin))
            {
                string expectedOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
                if (origin.Count != 1 ||
                    !string.Equals(origin[0], expectedOrigin, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
            }

            return next(context);
        });
    }

    /// <summary>
    /// Ignores <see cref="OperationCanceledException"/> exceptions when
    /// <see cref="HttpContext.RequestAborted"/> is signaled.
    /// </summary>
    /// <remarks>
    /// Place this immediately after exception handling or logging middleware, such as
    /// <see cref="DeveloperExceptionPageExtensions.UseDeveloperExceptionPage(IApplicationBuilder)">UseDeveloperExceptionPage</see>.
    /// </remarks>
    public static IApplicationBuilder UseIgnoreDisconnections(this IApplicationBuilder builder)
    {
        return builder.Use(static next =>
        {
            return async context =>
            {
                try
                {
                    await next(context);
                }
                catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
                {
                }
            };
        });
    }
}

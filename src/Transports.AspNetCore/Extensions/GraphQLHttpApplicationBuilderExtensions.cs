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
    public static IApplicationBuilder UseGraphQL<TMiddleware>(this IApplicationBuilder builder, string path = "/graphql", params object[] args)
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
    public static IApplicationBuilder UseGraphQL<TMiddleware>(this IApplicationBuilder builder, PathString path, params object[] args)
        where TMiddleware : GraphQLHttpMiddleware
    {
        return builder.UseWhen(
            context => context.Request.Path.Equals(path),
            b => b.UseMiddleware<TMiddleware>(args));
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

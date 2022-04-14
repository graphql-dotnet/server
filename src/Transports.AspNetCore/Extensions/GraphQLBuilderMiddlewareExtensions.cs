#nullable enable

using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;

namespace GraphQL.Server;

/// <summary>
/// GraphQL specific extension methods for <see cref="IGraphQLBuilder"/>.
/// </summary>
public static class GraphQLBuilderMiddlewareExtensions
{
    public static IGraphQLBuilder AddHttpMiddleware<TSchema>(this IGraphQLBuilder builder)
      where TSchema : ISchema
    {
        builder.Services.Register<GraphQLHttpMiddleware<TSchema>, GraphQLHttpMiddleware<TSchema>>(ServiceLifetime.Singleton);
        return builder;
    }

    public static IGraphQLBuilder AddHttpMiddleware<TSchema, TMiddleware>(this IGraphQLBuilder builder)
        where TSchema : ISchema
        where TMiddleware : GraphQLHttpMiddleware<TSchema>
    {
        builder.Services.Register<TMiddleware, TMiddleware>(ServiceLifetime.Singleton);
        return builder;
    }
}

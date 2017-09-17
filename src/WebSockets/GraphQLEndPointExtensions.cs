using GraphQL.Http;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class GraphQLEndPointExtensions
    {
        public static IServiceCollection AddGraphQLEndPoint<TSchema>(this IServiceCollection services)
            where TSchema : Schema
        {
            services.AddSingleton<GraphQLEndPoint<TSchema>>();

            return services;
        }

        public static IServiceCollection AddGraphQL(this IServiceCollection services)
        {
            services.AddSingleton<IDocumentWriter, DocumentWriter>();
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddSingleton<ISubscriptionExecuter, SubscriptionExecuter>();

            return services;
        }

        public static IApplicationBuilder UseGraphQLEndPoint<TSchema>(this IApplicationBuilder builder,
            string path)
            where TSchema : Schema
        {
            builder.Use(async (context, next) =>
                {
                    if (context.Request.Path == path && context.WebSockets.IsWebSocketRequest)
                    {
                        var socket = await context.WebSockets.AcceptWebSocketAsync(GraphQLConnectionContext.Protocol);
                        var connection = new GraphQLConnectionContext(socket, context.Connection.Id);
                        var endpoint = context.RequestServices.GetRequiredService<GraphQLEndPoint<TSchema>>();
                        await endpoint.OnConnectedAsync(connection);
                    }

                    await next();
                }
            );

            return builder;
        }
    }
}

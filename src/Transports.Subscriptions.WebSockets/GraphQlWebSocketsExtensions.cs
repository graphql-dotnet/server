using GraphQL.Server.Core;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class GraphQLWebSocketsExtensions
    {
        public static IGraphQLBuilder AddWebSockets(this IGraphQLBuilder builder)
        {
            builder.Services
                .AddTransient<IOperationMessageListener, LogMessagesListener>()
                .AddTransient<IOperationMessageListener, ProtocolMessageListener>();

            return builder;
        }

        public static IApplicationBuilder UseGraphQLWebSocket<TSchema>(this IApplicationBuilder builder,
            PathString path)
            where TSchema : ISchema
        {
            return builder.Map(path, x => x.UseMiddleware<GraphQLWebSocketsMiddleware<TSchema>>());
        }
    }
}
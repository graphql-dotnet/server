using System;
using System.Collections.Generic;
using GraphQL.Http;
using GraphQL.Subscription;
using GraphQL.Transports.AspNetCore.Abstractions;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Transports.AspNetCore
{
    public static class GraphQLExtensions
    {
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
                if (context.Request.Path == path)
                {
                    var transports = context.RequestServices.GetServices<ITransport<TSchema>>();
                    var transport = GetFirstAcceptedTransport(transports, context);

                    if (transport == null)
                        throw new InvalidOperationException(
                            $"No transport found for {typeof(TSchema).FullName} found.");

                    await transport.OnConnectedAsync(context);
                }
                else
                {
                    await next();
                }
            });

            return builder;
        }

        private static ITransport<TSchema> GetFirstAcceptedTransport<TSchema>(IEnumerable<ITransport<TSchema>> transports, HttpContext context) where TSchema : Schema
        {
            foreach (var transport in transports)
            {
                var accepts = transport.Accepts(context);

                if (accepts)
                    return transport;
            }

            return null;
        }
    }
}

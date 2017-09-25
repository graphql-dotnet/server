using System.IO;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class GraphQLEndPointExtensions
    {
        public static IServiceCollection AddGraphQLEndPoint<TSchema>(this IServiceCollection services)
            where TSchema : Schema
        {
            services.AddSingleton<ISubscriptionMessageProtocol<TSchema>, SubscriptionMessageProtocol<TSchema>>();
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
                    if (context.Request.Path == path)
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            var socket =
                                await context.WebSockets.AcceptWebSocketAsync(GraphQLConnectionContext.Protocol);
                            var connection = new GraphQLConnectionContext(socket, context.Connection.Id);
                            var endpoint = context.RequestServices.GetRequiredService<GraphQLEndPoint<TSchema>>();
                            await endpoint.OnConnectedAsync(connection);
                        }
                        else
                        {
                            var documentExecuter = context.RequestServices.GetRequiredService<IDocumentExecuter>();
                            var documentWriter = context.RequestServices.GetRequiredService<IDocumentWriter>();
                            var query = await GetQueryAsync(context);
                            var result = await documentExecuter.ExecuteAsync(new ExecutionOptions
                            {
                                Schema = context.RequestServices.GetRequiredService<TSchema>(),
                                Query = query.Query,
                                OperationName = query.OperationName,
                                Inputs = query.GetInputs()
                            });

                            context.Response.StatusCode = 200;
                            context.Response.ContentType = "application/json";
                            await WriteResponseJson(context.Response.Body, result, documentWriter);
                        }
                    }
                    else
                    {
                        await next();
                    }   
                }
            );

            return builder;
        }

        private static async Task WriteResponseJson(Stream responseBody, ExecutionResult result, IDocumentWriter documentWriter)
        {
            var json = documentWriter.Write(result);

            using (var streamWriter = new StreamWriter(responseBody, Encoding.UTF8, 4069, true))
            {
                await streamWriter.WriteAsync(json);
                await streamWriter.FlushAsync();
            }
        }

        private static async Task<GraphQuery> GetQueryAsync(HttpContext context)
        {
            using (var reader = new StreamReader(context.Request.Body))
            {
                var json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<GraphQuery>(json);
            }
        }
    }
}

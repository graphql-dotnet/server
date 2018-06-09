using GraphQL.Http;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.AspNetCore
{
    public static class GraphQLExtensions
    {
        /// <summary>
        /// Adds the GraphQLHttp to services
        /// </summary>
        /// <param name="services">The service collection to register the services on</param>
        /// <returns>The <see cref="IServiceCollection"/> received as parameter</returns>
        public static IServiceCollection AddGraphQLHttp(this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentWriter>(
                x =>
                {
                    var hostingEnvironment = x.GetRequiredService<IHostingEnvironment>();
                    var jsonSerializerSettings = x.GetRequiredService<IOptions<JsonSerializerSettings>>();
                    return new DocumentWriter(
                        hostingEnvironment.IsDevelopment() ? Formatting.Indented : Formatting.None,
                        jsonSerializerSettings.Value);
                });
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();

            return services;
        }

        /// <summary>
        /// Adds the GraphQLHttp to services
        /// </summary>
        /// <typeparam name="TUserContextBuilder">The <see cref="IUserContextBuilder"/> to use for generating the userContext used for the GraphQL request</typeparam>
        /// <param name="services">The service collection to register the services on</param>
        /// <returns>The <see cref="IServiceCollection"/> received as parameter</returns>
        public static IServiceCollection AddGraphQLHttp<TUserContextBuilder>(this IServiceCollection services)
            where TUserContextBuilder : class, IUserContextBuilder
        {
            services.AddGraphQLHttp();
            services.TryAddTransient<IUserContextBuilder, TUserContextBuilder>();

            return services;
        }

        /// <summary>
        /// Use the GraphQLHttp middleware with the provided options
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> to use the middleware on</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQLHttp<TSchema>(this IApplicationBuilder builder,
            GraphQLHttpOptions graphQLHttpOptions)
            where TSchema : ISchema
        {
            builder.UseMiddleware<GraphQLHttpMiddleware<TSchema>>(Options.Create(graphQLHttpOptions));

            return builder;
        }
    }
}

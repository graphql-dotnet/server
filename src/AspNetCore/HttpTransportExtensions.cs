using GraphQL.Transports.AspNetCore.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Transports.AspNetCore
{
    public static class HttpTransportExtensions
    {
        public static IServiceCollection AddGraphQLHttpTransport<TSchema>(this IServiceCollection services) where TSchema : Schema
        {
            services.AddSingleton<ITransport<TSchema>, HttpRequestTransport<TSchema>>();
            return services;
        }
    }
}

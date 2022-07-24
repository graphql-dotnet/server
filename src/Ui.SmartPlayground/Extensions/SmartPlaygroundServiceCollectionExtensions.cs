using GraphQL.Server.Ui.SmartPlayground;
using GraphQL.Server.Ui.SmartPlayground.Smart;
using Microsoft.Extensions.DependencyInjection;
using OAuth2.Infrastructure;

namespace Microsoft.AspNetCore.Builder
{
    public static class SmartPlaygroundServiceCollectionExtensions
    {
        public static void AddSmartPlayground(this IServiceCollection services)
        {
            services.AddHttpClient("SMARTHttpClient");
            services.AddSingleton<SmartPlaygroundOptions>();
            services.AddSingleton<IRequestFactory, RequestFactory>();
            services.AddTransient<ISmartClient, SmartClient>();
            var serviceCollection = services.AddSingleton<Func<ISmartClient>>(p => () => p.GetRequiredService<ISmartClient>());
        }
    }
}

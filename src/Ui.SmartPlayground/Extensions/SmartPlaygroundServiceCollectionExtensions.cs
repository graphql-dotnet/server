using GraphQL.Server.Ui.SmartPlayground.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class SmartPlaygroundServiceCollectionExtensions
    {
        public static void AddSmartPlayground(this IServiceCollection services)
        {
            services.AddHttpClient("SMARTHttpClient");
            services.AddSingleton<ISmartClientFactory, SmartClientFactory>();
        }
    }
}

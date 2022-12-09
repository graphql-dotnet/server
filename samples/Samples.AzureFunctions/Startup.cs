using GraphQL;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Chat = GraphQL.Samples.Schemas.Chat;

[assembly: FunctionsStartup(typeof(Samples.AzureFunctions.Startup))]
namespace Samples.AzureFunctions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();

        builder.Services.AddGraphQL(b => b
            .AddAutoSchema<Chat.Query>(s => s
                .WithMutation<Chat.Mutation>()
                .WithSubscription<Chat.Subscription>())
            .AddSystemTextJson());
    }
}

using System;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using GraphQL;
using GraphQL.Server;
using Demo.Azure.Functions.GraphQL.Schema;
using Demo.Azure.Functions.GraphQL.Infrastructure;

[assembly: FunctionsStartup(typeof(Demo.Azure.Functions.GraphQL.Startup))]

namespace Demo.Azure.Functions.GraphQL
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IDocumentClient>(serviceProvider => {
                DbConnectionStringBuilder cosmosDBConnectionStringBuilder = new DbConnectionStringBuilder
                {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>()[Constants.CONNECTION_STRING_SETTING]
                };

                if (cosmosDBConnectionStringBuilder.TryGetValue("AccountKey", out object accountKey) && cosmosDBConnectionStringBuilder.TryGetValue("AccountEndpoint", out object accountEndpoint))
                {
                    return new DocumentClient(new Uri(accountEndpoint.ToString()), accountKey.ToString());

                }

                return null;
            });

            builder.Services.AddScoped<IDependencyResolver>(serviceProvider => new FuncDependencyResolver(serviceProvider.GetRequiredService));
            builder.Services.AddScoped<StarWarsSchema>();

            builder.Services.AddSingleton<IDocumentExecuter>(new DocumentExecuter());
            builder.Services.AddGraphQL(options =>
            {
                options.ExposeExceptions = true;
            })
            .AddGraphTypes(ServiceLifetime.Scoped)
            .AddDataLoader();
        }
    }
}

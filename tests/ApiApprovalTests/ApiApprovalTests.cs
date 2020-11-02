using System;
using PublicApiGenerator;
using Shouldly;
using Xunit;

namespace GraphQL.Authorization.ApiTests
{
    /// <see href="https://github.com/JakeGinnivan/ApiApprover"/>
    public class ApiApprovalTests
    {
        [Theory]
        [InlineData(typeof(Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequestDeserializer))]
        [InlineData(typeof(Server.Transports.AspNetCore.SystemTextJson.GraphQLRequestDeserializer))]
        [InlineData(typeof(Server.Ui.Altair.AltairMiddleware))]
        [InlineData(typeof(Server.Ui.GraphiQL.GraphiQLMiddleware))]
        [InlineData(typeof(Server.Ui.Playground.PlaygroundMiddleware))]
        [InlineData(typeof(Server.Ui.Voyager.VoyagerMiddleware))]
        [InlineData(typeof(Server.Authorization.AspNetCore.AuthorizationValidationRule))]
        [InlineData(typeof(Server.GraphQLRequest))]
        [InlineData(typeof(Server.Transports.AspNetCore.GraphQLHttpMiddleware<>))]
        [InlineData(typeof(Server.Transports.Subscriptions.Abstractions.SubscriptionServer))]
        [InlineData(typeof(Server.Transports.WebSockets.WebSocketTransport))]
        public void public_api_should_not_change_unintentionally(Type type)
        {
            string publicApi = type.Assembly.GeneratePublicApi(new ApiGeneratorOptions
            {
                IncludeAssemblyAttributes = false,
                WhitelistedNamespacePrefixes = new[] { "Microsoft." }
            });

            // See: https://shouldly.readthedocs.io/en/latest/assertions/shouldMatchApproved.html
            // Note: If the AssemblyName.approved.txt file doesn't match the latest publicApi value,
            // this call will try to launch a diff tool to help you out but that can fail on
            // your machine if a diff tool isn't configured/setup. 
            publicApi.ShouldMatchApproved(options => options.WithFilenameGenerator((testMethodInfo, discriminator, fileType, fileExtension) => $"{type.Assembly.GetName().Name}.{fileType}.{fileExtension}"));
        }
    }
}

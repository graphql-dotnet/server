using System;
using System.Linq;
using GraphQL;
using GraphQL.Samples.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Samples.Server.Tests
{
    public class DITest
    {
        [Fact]
        public void Services_Should_Contain_Only_One_DocumentWriter()
        {
            var cfg = new ConfigurationBuilder().Build();
            var env = (IWebHostEnvironment)Activator.CreateInstance(Type.GetType("Microsoft.AspNetCore.Hosting.HostingEnvironment, Microsoft.AspNetCore.Hosting"));
            var startup = new Startup(cfg, env);
            var services = new ServiceCollection();
            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();
            var serializers = provider.GetServices<IGraphQLSerializer>();
            serializers.Count().ShouldBe(1);
        }
    }
}

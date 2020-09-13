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
#if NETCOREAPP2_2
            var env = new Microsoft.AspNetCore.Hosting.Internal.HostingEnvironment();
#else
            
            var env = (IWebHostEnvironment)Activator.CreateInstance(Type.GetType("Microsoft.AspNetCore.Hosting.HostingEnvironment, Microsoft.AspNetCore.Hosting"));
#endif
            var startup = new Startup(cfg, env);
            var services = new ServiceCollection();
            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();
            var writers = provider.GetServices<IDocumentWriter>();
            writers.Count().ShouldBe(1);
        }
    }
}

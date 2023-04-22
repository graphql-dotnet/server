using GraphQL.Server.Samples.Net48;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Samples.Tests;

namespace Samples.Net48.Tests;

public class EndToEndTests : IClassFixture<EndToEndTests.Fixture>
{
    private readonly TestServer _testServer;

    public EndToEndTests(Fixture fixture)
    {
        _testServer = fixture.TestServer;
    }

    [Fact]
    public async Task RootRedirect()
    {
        using var client = _testServer.CreateClient();
        using var response = await client.GetAsync("/");
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Found);
        response.Headers.TryGetValues("Location", out var values).ShouldBeTrue();
        values.ShouldHaveSingleItem().ShouldBe("/ui/graphql");
    }

    [Fact]
    public Task GraphiQL()
        => _testServer.VerifyGraphiQLAsync("/ui/graphql");

    [Fact]
    public Task GraphQLGet()
        => _testServer.VerifyGraphQLGetAsync();

    [Fact]
    public Task GraphQLPost()
        => _testServer.VerifyGraphQLPostAsync();

    [Fact]
    public Task GraphQLWebSockets()
        => _testServer.VerifyGraphQLWebSocketsAsync();

    public class Fixture : IDisposable
    {
        public Fixture()
        {
            var builder = new WebHostBuilder()
                .UseEnvironment("Development")
                .UseStartup<Startup>();

            TestServer = new TestServer(builder);
        }

        public TestServer TestServer { get; }

        public void Dispose() => TestServer.Dispose();
    }
}

using System.Net;
using System.Net.WebSockets;
#if !NETCOREAPP2_1 && !NET48
using Microsoft.AspNetCore.Http.Features;
#endif
using Microsoft.Extensions.Primitives;

namespace Tests.Middleware;

public class WebSocketSameOriginTests
{
    private static TestServer CreateServer()
    {
        return new TestServer(new WebHostBuilder().Configure(app =>
        {
            app.UseWebSocketsWithSameOriginCheck();

            app.Run(async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            });
        }));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("https://example.com:8443", true)]
    [InlineData("HTTPS://EXAMPLE.COM:8443", true)]
    [InlineData("http://example.com:8443", false)]
    [InlineData("https://example.com", false)]
    [InlineData("https://example.com:9443", false)]
    [InlineData("https://other.example:8443", false)]
    [InlineData("https://example.com.evil.example:8443", false)]
    [InlineData("https://example.com:8443@evil.example", false)]
    [InlineData("https://example.com:8443/path", false)]
    [InlineData("https://example.com:8443/", false)]
    [InlineData("null", false)]
    [InlineData("", false)]
    public async Task ValidatesWebSocketOrigin(string? origin, bool allowed)
    {
        using var server = CreateServer();
        var client = server.CreateWebSocketClient();
        if (origin != null)
            client.ConfigureRequest = request => request.Headers["Origin"] = origin;

        if (allowed)
        {
            using var socket = await client.ConnectAsync(new Uri("https://example.com:8443/graphql"), CancellationToken.None);
        }
        else
        {
            var error = await Should.ThrowAsync<InvalidOperationException>(() =>
                client.ConnectAsync(new Uri("https://example.com:8443/graphql"), CancellationToken.None));
            error.Message.ShouldBe("Incomplete handshake, status code: 403");
        }
    }

    [Theory]
    [InlineData("http://localhost/graphql", "http://localhost")]
    [InlineData("https://example.com/graphql", "https://example.com")]
    [InlineData("http://[::1]:5000/graphql", "http://[::1]:5000")]
    public async Task UsesRequestSchemeAndHost(string url, string origin)
    {
        using var server = CreateServer();
        var client = server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers["Origin"] = origin;
        using var socket = await client.ConnectAsync(new Uri(url), CancellationToken.None);
    }

    [Fact]
    public async Task RejectsMultipleOrigins()
    {
        using var server = CreateServer();
        var client = server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers["Origin"] = new StringValues(new[]
        {
            "https://example.com:8443", "https://other.example"
        });

        var error = await Should.ThrowAsync<InvalidOperationException>(() =>
            client.ConnectAsync(new Uri("https://example.com:8443/graphql"), CancellationToken.None));
        error.Message.ShouldBe("Incomplete handshake, status code: 403");
    }

#if !NETCOREAPP2_1 && !NET48
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AppliesSuppliedWebSocketOptions(bool allowed)
    {
        var options = new WebSocketOptions
        {
            AllowedOrigins = { allowed ? "https://example.com:8443" : "https://other.example" }
        };
        // TestServer supplies its own WebSocket feature, bypassing ASP.NET Core's options.
        // Use an upgrade feature here to exercise the underlying WebSocket middleware too.
        using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        var builder = new ApplicationBuilder(provider);
        builder.UseWebSocketsWithSameOriginCheck(options).ShouldBeSameAs(builder);
        builder.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Features.Set(Mock.Of<IHttpUpgradeFeature>(feature => feature.IsUpgradableRequest));
        context.Request.Method = "GET";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("example.com", 8443);
        context.Request.Headers["Connection"] = "Upgrade";
        context.Request.Headers["Upgrade"] = "websocket";
        context.Request.Headers["Sec-WebSocket-Version"] = "13";
        context.Request.Headers["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==";
        context.Request.Headers["Origin"] = "https://example.com:8443";

        await builder.Build()(context);
        context.Response.StatusCode.ShouldBe(allowed ? StatusCodes.Status204NoContent : StatusCodes.Status403Forbidden);
    }
#endif

    [Fact]
    public async Task DoesNotRestrictHttpRequests()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com:8443/graphql");
        request.Headers.Add("Origin", "https://other.example");
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}

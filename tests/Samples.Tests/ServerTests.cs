using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Samples.Tests;

public class ServerTests<TProgram> where TProgram : class
{
    public async Task VerifyPlaygroundAsync(string url = "/")
    {
        using var webApp = new WebApplicationFactory<TProgram>();
        await webApp.Server.VerifyPlaygroundAsync(url);
    }

    public async Task VerifyGraphiQLAsync(string url = "/")
    {
        using var webApp = new WebApplicationFactory<TProgram>();
        await webApp.Server.VerifyGraphiQLAsync(url);
    }

    public async Task VerifyGraphQLGetAsync(string url = "/graphql", string query = "{count}", string expected = @"{""data"":{""count"":0}}", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        using var webApp = new WebApplicationFactory<TProgram>();
        await webApp.Server.VerifyGraphQLGetAsync(url, query, expected, statusCode);
    }

    public async Task VerifyGraphQLPostAsync(string url = "/graphql", string query = "{count}", string expected = @"{""data"":{""count"":0}}", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        using var webApp = new WebApplicationFactory<TProgram>();
        await webApp.Server.VerifyGraphQLPostAsync(url, query, expected, statusCode);
    }

    public async Task VerifyGraphQLWebSocketsAsync(string url = "/graphql", string query = "{count}", string expected = @"{""data"":{""count"":0}}", bool success = true)
    {
        using var webApp = new WebApplicationFactory<TProgram>();
        await webApp.Server.VerifyGraphQLWebSocketsAsync(url, query, expected, success);
    }
}

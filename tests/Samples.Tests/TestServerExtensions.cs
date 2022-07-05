using System.Net;
using GraphQL.Transport;
using Microsoft.AspNetCore.TestHost;

namespace Samples.Tests;

public static class TestServerExtensions
{
    public static async Task VerifyPlaygroundAsync(this TestServer server, string url = "/")
    {
        using var client = server.CreateClient();
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldContain(@"<!DOCTYPE html>", Case.Insensitive);
        ret.ShouldContain("playground", Case.Insensitive);
    }

    public static async Task VerifyGraphiQLAsync(this TestServer server, string url = "/")
    {
        using var client = server.CreateClient();
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldContain(@"<!DOCTYPE html>", Case.Insensitive);
        ret.ShouldContain("graphiql", Case.Insensitive);
    }

    public static async Task VerifyGraphQLGetAsync(this TestServer server, string url = "/graphql", string query = "{count}", string expected = @"{""data"":{""count"":0}}", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        using var client = server.CreateClient();
        using var response = await client.GetAsync(url + "?query=" + Uri.EscapeDataString(query));
        response.StatusCode.ShouldBe(statusCode);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe(expected);
    }

    public static async Task VerifyGraphQLPostAsync(this TestServer server, string url = "/graphql", string query = "{count}", string expected = @"{""data"":{""count"":0}}", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        using var client = server.CreateClient();
        var request = System.Text.Json.JsonSerializer.Serialize(new { query });
        var content = new StringContent(request, System.Text.Encoding.UTF8, "application/graphql+json");
        using var response = await client.PostAsync(url, content);
        response.StatusCode.ShouldBe(statusCode);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe(expected);
    }

    public static async Task VerifyGraphQLWebSocketsAsync(this TestServer server, string url = "/graphql", string query = "{count}", string expected = @"{""data"":{""count"":0}}", bool success = true)
    {
        var webSocketClient = server.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
        {
            request.Headers["Sec-WebSocket-Protocol"] = "graphql-transport-ws";
        };
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var webSocket = await webSocketClient.ConnectAsync(new Uri(server.BaseAddress, url), default);

        // send CONNECTION_INIT
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "connection_init"
        });

        // wait for CONNECTION_ACK
        var message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe("connection_ack");

        // send query
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Id = "1",
            Type = "subscribe",
            Payload = new GraphQLRequest
            {
                Query = query
            }
        });

        // wait for response
        message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe(success ? "next" : "error");
        message.Id.ShouldBe("1");
        message.Payload.ShouldBe(expected);

        if (success)
        {
            // wait for complete
            message = await webSocket.ReceiveMessageAsync();
            message.Type.ShouldBe("complete");
            message.Id.ShouldBe("1");
        }
    }
}

namespace Tests;

internal static class TestServerExtensions
{
    public static async Task<string> ExecuteGet(this TestServer server, string url)
    {
        var client = server.CreateClient();
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var str = await response.Content.ReadAsStringAsync();
        return str;
    }

    public static async Task<string> ExecutePost(this TestServer server, string url, string query, object? variables = null)
    {
        var client = server.CreateClient();
        var data = System.Text.Json.JsonSerializer.Serialize(new { query = query, variables = variables });
        var content = new StringContent(data, Encoding.UTF8, "application/graphql+json");
        using var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var str = await response.Content.ReadAsStringAsync();
        return str;
    }

    public static async Task VerifyChatSubscriptionAsync(this TestServer server, string url = "/graphql")
    {
        // create websocket connection
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

        // subscribe
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "subscribe",
            Id = "123",
            Payload = new GraphQLRequest
            {
                Query = "subscription { events { type message { id message from } } }",
            },
        });

        // It is necessary to allow time for the asynchronous websocket handler code
        // to execute prior to the independent call to AddMessageInternal below.
        // Since the websocket call does not return a response when the subscription
        // has completed being set up, there is no response we can await to determine
        // when to call AddMessageInternal; so for the purposes of testing, we make
        // an additional call here.

        // wait for the message to be handled by the server;
        // just send a ping and wait for the pong
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "ping",
        });
        message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe("pong");

        // post a new message
        {
            var str = await server.ExecutePost(
                url,
                "mutation {addMessage(message:{message:\"hello\",from:\"John Doe\"}){id}}");
            str.ShouldBe("{\"data\":{\"addMessage\":{\"id\":\"1\"}}}");
        }

        // wait for a new message sent over this websocket
        message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe("next");
        message.Payload.ShouldBe(@"{""data"":{""events"":{""type"":""NEW_MESSAGE"",""message"":{""id"":""1"",""message"":""hello"",""from"":""John Doe""}}}}");

        // unsubscribe
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "complete",
            Id = "123",
        });

        // close websocket
        await webSocket.CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, null, default);

        // wait for websocket closure
        (await webSocket.ReceiveCloseAsync()).ShouldBe(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure);
    }
}

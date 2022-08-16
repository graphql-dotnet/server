using System.Net.WebSockets;
using System.Text.Json;
#if NET48
using MemoryBytes = System.ArraySegment<byte>;
using ValueWebSocketReceiveResult = System.Net.WebSockets.WebSocketReceiveResult;
#else
using MemoryBytes = System.Memory<byte>;
#endif

namespace Tests;

public static class WebSocketExtensions
{
    private static readonly IGraphQLTextSerializer _serializer = new GraphQLSerializer();

    public static Task SendMessageAsync(this WebSocket socket, OperationMessage message)
        => SendStringAsync(socket, _serializer.Serialize(message));

    public static async Task SendStringAsync(this WebSocket socket, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        await socket.SendAsync(new MemoryBytes(bytes), WebSocketMessageType.Text, true, default);
    }

    public static async Task<OperationMessage> ReceiveMessageAsync(this WebSocket socket)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(5000);
        var mem = new MemoryStream();
        ValueWebSocketReceiveResult response;
        do
        {
            var buffer = new byte[1024];
            response = await socket.ReceiveAsync(new MemoryBytes(buffer), cts.Token);
            mem.Write(buffer, 0, response.Count);
        } while (!response.EndOfMessage);
        response.MessageType.ShouldBe(WebSocketMessageType.Text);
        mem.Position = 0;
        var message = await _serializer.ReadAsync<OperationMessage>(mem);
        if (message!.Payload != null)
            message.Payload = ((JsonElement)message.Payload).GetRawText();
        return message;
    }

    public static async Task<WebSocketCloseStatus> ReceiveCloseAsync(this WebSocket socket)
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(5000);
        var mem = new MemoryStream();
        ValueWebSocketReceiveResult response;
        do
        {
            var buffer = new byte[1024];
            response = await socket.ReceiveAsync(new MemoryBytes(buffer), cts.Token);
            mem.Write(buffer, 0, response.Count);
        } while (!response.EndOfMessage);
        response.MessageType.ShouldBe(WebSocketMessageType.Close);
        return socket.CloseStatus!.Value;
    }
}

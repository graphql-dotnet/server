namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

public class WebSocketHandlerOptions
{
    public TimeSpan ConnectionInitWaitTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane;

public class WebSocketHandlerOptions
{
    public TimeSpan ConnectionInitWaitTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan KeepAliveTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

namespace GraphQL.Server.Transports.WebSockets.Tests;

public class TestMessage
{
    public string Content { get; set; }

    public DateTimeOffset SentAt { get; set; }
}

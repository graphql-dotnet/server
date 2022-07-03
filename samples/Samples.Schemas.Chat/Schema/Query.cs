namespace GraphQL.Samples.Schemas.Chat;

public class Query
{
    public static Message? LastMessage([FromServices] IChat chatService)
        => chatService.LastMessage;

    public static IEnumerable<Message> AllMessages([FromServices] IChat chatService, string? from = null)
        => from == null ? chatService.GetAllMessages() : chatService.GetMessageFromUser(from);

    public static int Count([FromServices] IChat chatService)
        => chatService.Count;
}

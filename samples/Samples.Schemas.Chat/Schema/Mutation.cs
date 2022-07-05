namespace GraphQL.Samples.Schemas.Chat;

public class Mutation
{
    public static Message AddMessage([FromServices] IChat chatService, MessageInput message)
        => chatService.PostMessage(message);

    public static Message? DeleteMessage([FromServices] IChat chatService, [Id] int id)
        => chatService.DeleteMessage(id);

    public static int ClearMessages([FromServices] IChat chatService)
        => chatService.ClearMessages();
}

namespace GraphQL.Samples.Schemas.Chat;

public class Subscription
{
    public static IObservable<Message> NewMessages([FromServices] IChat chatService, string? from = null)
        => from == null ? chatService.SubscribeAll() : chatService.SubscribeFromUser(from);

    public static IObservable<Event> Events([FromServices] IChat chatService)
        => chatService.SubscribeEvents();
}

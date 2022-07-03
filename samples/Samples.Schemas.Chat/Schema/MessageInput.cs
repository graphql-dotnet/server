namespace GraphQL.Samples.Schemas.Chat;

public class MessageInput
{
    public string Message { get; set; } = null!;
    public string From { get; set; } = null!;
}

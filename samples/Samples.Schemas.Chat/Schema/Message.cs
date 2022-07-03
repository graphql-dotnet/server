namespace GraphQL.Samples.Schemas.Chat;

public class Message
{
    [Id]
    public int Id { get; set; }

    [Name("Message")]
    public string Value { get; set; } = null!;

    public string From { get; set; } = null!;

    public DateTime Sent { get; set; }
}

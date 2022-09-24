namespace GraphQL.Samples.Schemas.Chat;

public class ChatSchema : Schema
{
    public ChatSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<Query>();
        Mutation = new AutoRegisteringObjectGraphType<Mutation>();
        Subscription = new AutoRegisteringObjectGraphType<Subscription>();
    }
}

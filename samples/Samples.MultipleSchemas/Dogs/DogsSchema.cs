namespace MultipleSchema.Dogs;

public class DogsSchema : Schema
{
    public DogsSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = new AutoRegisteringObjectGraphType<Query>();
        Mutation = new AutoRegisteringObjectGraphType<Mutation>();
        Subscription = new AutoRegisteringObjectGraphType<Subscription>();
    }
}

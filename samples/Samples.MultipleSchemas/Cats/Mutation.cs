namespace MultipleSchema.Cats;

public class Mutation
{
    public static Cat Add([FromServices] CatsData cats, string name, string breed)
        => cats.Add(name, breed);

    public static Cat? Remove([FromServices] CatsData cats, [Id] int id)
        => cats.Remove(id);
}

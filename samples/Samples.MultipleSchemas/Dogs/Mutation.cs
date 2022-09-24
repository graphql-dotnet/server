namespace MultipleSchema.Dogs;

public class Mutation
{
    public static Dog Add([FromServices] DogsData dogs, string name, string breed)
        => dogs.Add(name, breed);

    public static Dog? Remove([FromServices] DogsData dogs, [Id] int id)
        => dogs.Remove(id);
}

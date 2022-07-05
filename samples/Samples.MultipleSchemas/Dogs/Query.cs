namespace MultipleSchema.Dogs;

public class Query
{
    public static Dog? Dog([FromServices] DogsData dogs, [Id] int id)
        => dogs[id];

    public static IEnumerable<Dog> Dogs([FromServices] DogsData dogs)
        => dogs.Dogs;
}

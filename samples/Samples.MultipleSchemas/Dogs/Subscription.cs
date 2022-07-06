namespace MultipleSchema.Dogs;

public class Subscription
{
    public static IObservable<Dog> Dogs([FromServices] DogsData dogs)
        => dogs.DogObservable();
}

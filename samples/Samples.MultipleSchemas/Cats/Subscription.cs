namespace MultipleSchema.Cats;

public class Subscription
{
    public static IObservable<Cat> Cats([FromServices] CatsData cats)
        => cats.CatObservable();
}

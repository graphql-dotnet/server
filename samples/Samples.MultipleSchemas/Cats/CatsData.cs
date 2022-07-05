using System.Reactive.Linq;

namespace MultipleSchema.Cats;

public class CatsData
{
    private readonly List<Cat> _cats = new() {
        new Cat(1, "Fluffy", "Himalayan"),
        new Cat(2, "Jasmine", "Persian"),
        new Cat(3, "Angie", "Maine Coon"),
    };
    private int _id = 3;

    public IEnumerable<Cat> Cats
    {
        get
        {
            IEnumerable<Cat> cats;
            lock (_cats)
                cats = _cats.ToList();
            return cats;
        }
    }

    public Cat? this[int id]
    {
        get
        {
            Cat? cat = null;
            lock (_cats)
                cat = _cats.FirstOrDefault(c => c.Id == id);
            return cat;
        }
    }

    public Cat Add(string name, string breed)
    {
        var cat = new Cat(Interlocked.Increment(ref _id), name, breed);
        lock (_cats)
            _cats.Add(cat);
        return cat;
    }

    public Cat? Remove(int id)
    {
        Cat? cat = null;
        lock (_cats)
        {
            for (int i = 0; i < _cats.Count; i++)
            {
                if (_cats[i].Id == id)
                {
                    cat = _cats[i];
                    _cats.RemoveAt(i);
                    break;
                }
            }
        }
        return cat;
    }

    public IObservable<Cat> CatObservable()
        => Cats.ToObservable();
}

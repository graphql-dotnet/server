using System.Reactive.Linq;

namespace MultipleSchema.Dogs;

public class DogsData
{
    private readonly List<Dog> _dogs = new() {
        new Dog(1, "Shadow", "Golden Retriever"),
        new Dog(2, "Chance", "American Bulldog"),
        new Dog(3, "Lassie", "Collie"),
    };
    private int _id = 3;

    public IEnumerable<Dog> Dogs
    {
        get
        {
            IEnumerable<Dog> dogs;
            lock (_dogs)
                dogs = _dogs.ToList();
            return dogs;
        }
    }

    public Dog? this[int id]
    {
        get
        {
            Dog? dog = null;
            lock (_dogs)
                dog = _dogs.FirstOrDefault(c => c.Id == id);
            return dog;
        }
    }

    public Dog Add(string name, string breed)
    {
        var dog = new Dog(Interlocked.Increment(ref _id), name, breed);
        lock (_dogs)
            _dogs.Add(dog);
        return dog;
    }

    public Dog? Remove(int id)
    {
        Dog? dog = null;
        lock (_dogs)
        {
            for (int i = 0; i < _dogs.Count; i++)
            {
                if (_dogs[i].Id == id)
                {
                    dog = _dogs[i];
                    _dogs.RemoveAt(i);
                    break;
                }
            }
        }
        return dog;
    }

    public IObservable<Dog> DogObservable()
        => Dogs.ToObservable();
}

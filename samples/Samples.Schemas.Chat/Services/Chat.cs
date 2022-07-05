using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GraphQL.Samples.Schemas.Chat;

public class Chat : IChat
{
    private readonly List<Message> _messages = new();
    private int _messageId;
    private readonly Subject<Event> _broadcaster = new();

    public Message? LastMessage { get; private set; }

    public IEnumerable<Message> GetAllMessages()
    {
        lock (_messages)
            return _messages.ToList();
    }

    public IEnumerable<Message> GetMessageFromUser(string from)
    {
        lock (_messages)
            return _messages.Where(x => string.Equals(x.From, from, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public Message PostMessage(MessageInput message)
    {
        var newMessage = new Message
        {
            Id = Interlocked.Increment(ref _messageId),
            Value = message.Message,
            From = message.From,
            Sent = DateTime.UtcNow,
        };
        LastMessage = newMessage;
        lock (_messages)
            _messages.Add(newMessage);
        _broadcaster.OnNext(new Event { Type = EventType.NewMessage, Message = newMessage });
        return newMessage;
    }

    public Message? DeleteMessage(int id)
    {
        Message? deletedMessage = null;
        lock (_messages)
        {
            for (int i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].Id == id)
                {
                    deletedMessage = _messages[i];
                    _messages.RemoveAt(i);
                    break;
                }
            }
        }
        if (deletedMessage != null)
            _broadcaster.OnNext(new Event { Type = EventType.DeleteMessage, Message = deletedMessage });
        return deletedMessage;
    }
    public IObservable<Message> SubscribeAll() => _broadcaster.Where(x => x.Type == EventType.NewMessage).Select(x => x.Message!);

    public IObservable<Message> SubscribeFromUser(string from)
        => SubscribeAll().Where(x => string.Equals(x.From, from, StringComparison.InvariantCultureIgnoreCase));

    public IObservable<Event> SubscribeEvents() => _broadcaster;

    public int ClearMessages()
    {
        int count;
        lock (_messages)
        {
            count = _messages.Count;
            _messages.Clear();
        }
        _broadcaster.OnNext(new Event { Type = EventType.ClearMessages });
        return count;
    }

    public int Count
    {
        get
        {
            lock (_messages)
                return _messages.Count;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;

namespace GraphQL.Server.Transports.WebSockets.Events
{
    public class SimpleEventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<string, ISubject<object>> _subjects;

        public SimpleEventAggregator()
        {
            _subjects = new ConcurrentDictionary<string, ISubject<object>>();
        }

        public IObservable<object> Subject(string eventType)
        {
            return GetSubject(eventType);
        }

        public void Publish(string eventType, object eventData)
        {
            var subject = GetSubject(eventType);
            subject.OnNext(eventData);
        }

        public IObservable<T> Subject<T>(string eventType)
        {
            return (IObservable<T>) Subject(eventType);
        }

        public void Publish<T>(string eventType, T eventData)
        {
            Publish(eventType, (object) eventData);
        }

        private ISubject<object> GetSubject(string eventType)
        {
            return _subjects.GetOrAdd(eventType, new Subject<object>());
        }
    }
}

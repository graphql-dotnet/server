using System;

namespace GraphQL.Server.Transports.WebSockets.Events
{
    public interface IEventAggregator
    {
        IObservable<object> Subject(string eventType);

        void Publish(string eventType, object eventData);

        IObservable<T> Subject<T>(string eventType);

        void Publish<T>(string eventType, T eventData);
    }
}

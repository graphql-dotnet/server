using System;
using System.Threading.Tasks;

namespace GraphQL.Transports.Subscriptions.Abstractions
{
    public interface IMessageTransport
    {
        Task<IObservable<Message>> OpenReadAsync();

        Task WriteAsync(Message message);
    }
}

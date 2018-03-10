using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IOperationMessageListener
    {
        Task OnHandleMessageAsync(IMessageTransport transport, OperationMessage message);

        Task OnMessageHandledAsync(IMessageTransport transport, OperationMessage message);
    }
}
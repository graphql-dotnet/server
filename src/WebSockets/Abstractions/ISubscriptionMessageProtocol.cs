using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Types;

namespace GraphQL.Server.Transports.WebSockets.Abstractions
{
    public interface ISubscriptionProtocolHandler<TSchema> where TSchema : Schema
    {
        Task HandleMessageAsync(OperationMessageContext context);

        Task HandleConnectionClosed(OperationMessageContext context);
    }
}

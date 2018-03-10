using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IMessageTransport
    {
        ISourceBlock<OperationMessage> CreateReader();

        ITargetBlock<OperationMessage> CreateWriter();

        Task CloseAsync();
    }
}
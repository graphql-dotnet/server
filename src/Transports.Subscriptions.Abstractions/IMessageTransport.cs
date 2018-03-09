using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IMessageTransport
    {
        ISourceBlock<OperationMessage> Reader { get; }

        ITargetBlock<OperationMessage> Writer { get; }

        Task Completion { get; }

        void Complete();
    }
}
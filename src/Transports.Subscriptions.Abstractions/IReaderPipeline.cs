using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IReaderPipeline
    {
        void LinkTo(ITargetBlock<OperationMessage> target);

        Task Complete();

        Task Completion { get; }
    }
}
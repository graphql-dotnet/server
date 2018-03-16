using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Pipeline providing the source of messages9
    /// </summary>
    public interface IReaderPipeline
    {
        /// <summary>
        ///     Completion task
        /// </summary>
        Task Completion { get; }

        /// <summary>
        ///     Link this pipeline to target propagating completion
        /// </summary>
        /// <param name="target"></param>
        void LinkTo(ITargetBlock<OperationMessage> target);

        /// <summary>
        ///     Complete the source of this pipeline
        /// </summary>
        /// <remarks>
        ///     Propagates completion from the source to the linked target
        /// </remarks>
        /// <returns></returns>
        Task Complete();
    }
}
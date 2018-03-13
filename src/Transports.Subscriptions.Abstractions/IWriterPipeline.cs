using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Pipeline for writing messages
    /// </summary>
    public interface IWriterPipeline
    {
        /// <summary>
        ///     Completion
        /// </summary>
        Task Completion { get; }

        /// <summary>
        ///     Synchronous write
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool Post(OperationMessage message);

        /// <summary>
        ///     Asynchronous write
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(OperationMessage message);

        /// <summary>
        ///     Complete this pipeline
        /// </summary>
        /// <returns></returns>
        Task Complete();
    }
}
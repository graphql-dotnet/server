using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Operation message listener
    /// </summary>
    public interface IOperationMessageListener
    {
        /// <summary>
        ///     Called before message is handled
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task OnBeforeHandleAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message);

        /// <summary>
        ///     Called after message has been handled according to the protocol
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task OnAfterHandleAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message);
    }
}
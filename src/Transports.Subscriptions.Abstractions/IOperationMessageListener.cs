using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IOperationMessageListener
    {
        Task OnHandleMessageAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message);

        Task OnMessageHandledAsync(IReaderPipeline reader, IWriterPipeline writer, OperationMessage message);
    }
}
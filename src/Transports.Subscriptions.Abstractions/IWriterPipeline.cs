using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IWriterPipeline
    {
        bool Post(OperationMessage message);

        Task SendAsync(OperationMessage message);

        Task Completion { get; }

        Task Complete();
    }
}
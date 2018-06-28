using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public interface IExecutionOptionsFactory
    {
        Task<ExecutionOptions> CreateExecutionOptionsAsync();
    }
}

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public interface IGraphQLRequest
    {
        string Query { get; set; }

        string Variables { get; set; }

        string OperationName { get; set; }
    }
}

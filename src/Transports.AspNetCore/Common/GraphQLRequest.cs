namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public class GraphQLRequest
    {
        public virtual string Query { get; set; }

        public virtual string Variables { get; set; }

        public virtual string OperationName { get; set; }
    }
}

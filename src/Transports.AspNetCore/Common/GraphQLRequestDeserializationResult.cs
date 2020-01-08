namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public class GraphQLRequestDeserializationResult
    {
        public bool WasSuccessful { get; set; }

        public IGraphQLRequest Single { get; set; }

        public IGraphQLRequest[] Batch { get; set; }
    }
}

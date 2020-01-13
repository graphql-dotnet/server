namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public class GraphQLRequestDeserializationResult
    {
        public bool WasSuccessful { get; set; }

        public GraphQLRequest Single { get; set; }

        public GraphQLRequest[] Batch { get; set; }
    }
}

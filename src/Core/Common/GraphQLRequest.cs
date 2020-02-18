namespace GraphQL.Server.Common
{
    public class GraphQLRequest
    {
        public const string OperationNameKey = "operationName";
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";

        public string OperationName { get; set; }
        public string Query { get; set; }
        public Inputs Inputs { get; set; }
    }
}

namespace GraphQL.Server.Common
{
    public class GraphQLRequest
    {
        public const string OPERATION_NAME_KEY = "operationName";
        public const string QUERY_KEY = "query";
        public const string VARIABLES_KEY = "variables";

        public string OperationName { get; set; }
        public string Query { get; set; }
        public Inputs Inputs { get; set; }
    }
}

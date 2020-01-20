namespace GraphQL.Server.Common
{
    public class GraphQLRequest
    {
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";
        public const string OperationNameKey = "operationName";

        public virtual string Query { get; set; }

        public virtual Inputs Variables { get; set; }

        public virtual string OperationName { get; set; }
    }
}

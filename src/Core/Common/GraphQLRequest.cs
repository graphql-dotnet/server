namespace GraphQL.Server.Common
{
    public abstract class GraphQLRequest
    {
        public const string OperationNameKey = "operationName";
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";

        public virtual string OperationName { get; set; }

        public virtual string Query { get; set; }

        public abstract Inputs GetInputs();
    }
}

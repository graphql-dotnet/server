namespace GraphQL.Server.Common
{
    public abstract class GraphQLRequest
    {
        public const string OperationNameKey = "operationName";
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";

        public virtual string OperationName { get; set; }

        public virtual string Query { get; set; }

        /// <summary>
        /// Returns an <see cref="Inputs"/> representing the variables
        /// passed in the request.
        /// </summary>
        /// <returns>Inputs, as deserialized from the request JSON.</returns>
        public abstract Inputs GetInputs();
    }
}

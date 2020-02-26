namespace GraphQL.Server
{
    /// <summary>
    /// A request for execution, see https://github.com/APIs-guru/graphql-over-http#request
    /// </summary>
    public class GraphQLRequest
    {
        public const string OPERATION_NAME_KEY = "operationName";
        public const string QUERY_KEY = "query";
        public const string VARIABLES_KEY = "variables";

        /// <summary>
        /// A document containing GraphQL operations and fragments to execute; required.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// The name of the operation in the document to execute; optional.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Values for any variables defined by the operation.
        /// </summary>
        public Inputs Inputs { get; set; }
    }
}

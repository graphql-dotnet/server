using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Payload of start message
    /// </summary>
    public class OperationMessagePayload
    {
        /// <summary>
        ///     Query, mutation or subsciption query
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        ///     Variables
        /// </summary>
        public JObject Variables { get; set; }

        /// <summary>
        ///     Operation name
        /// </summary>
        public string OperationName { get; set; }
    }
}
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     GraphQL operation messages
    /// </summary>
    public class OperationMessage
    {
        /// <summary>
        ///     Nullable Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Type <see cref="MessageType" />
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     Nullable payload
        /// </summary>
        public JObject Payload { get; set; }


        /// <inheritdoc />
        public override string ToString()
        {
            return $"Type: {Type} Id: {Id} Payload: {Payload}";
        }
    }
}
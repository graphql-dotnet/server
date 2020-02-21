using System.Runtime.Serialization;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    /// GraphQL operation messages
    /// </summary>
    [DataContract]
    public class OperationMessage
    {
        /// <summary>
        /// Nullable Id
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Type <see cref="MessageType" />
        /// </summary>
        [DataMember(Name = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Nullable payload
        /// </summary>
        [DataMember(Name = "payload")]
        public object Payload { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"Type: {Type} Id: {Id} Payload: {Payload}";
    }
}

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class OperationMessage
    {
        /// <summary>
        ///     Nullable Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Type <see cref="MessageType"/>
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///     Nullable payload
        /// </summary>
        public dynamic Payload { get; set; }
    }
}

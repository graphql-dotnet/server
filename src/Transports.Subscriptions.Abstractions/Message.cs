namespace GraphQL.Transports.Subscriptions.Abstractions
{
    public abstract class Message
    {
        public string Id { get; set; }

        public string Type { get; set; }

        public dynamic Payload { get; set; }
    }
}

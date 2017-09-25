using GraphQL.Server.Transports.WebSockets.Abstractions;

namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class OperationMessageContext
    {
        public OperationMessageContext(string connectionId, IJsonMessageWriter messageWriter, OperationMessage op)
        {
            ConnectionId = connectionId;
            MessageWriter = messageWriter;
            Op = op;
        }

        public string ConnectionId { get; protected set; }

        public IJsonMessageWriter MessageWriter { get; }
        public OperationMessage Op { get; }
    }
}

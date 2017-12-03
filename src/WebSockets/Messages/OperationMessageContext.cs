using GraphQL.Server.Transports.WebSockets.Abstractions;

namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class OperationMessageContext
    {
        private readonly IConnectionContext _connectionContext;

        public OperationMessageContext(IConnectionContext connectionContext, OperationMessage op)
        {
            _connectionContext = connectionContext;
            Op = op;
        }

        public string ConnectionId => _connectionContext.ConnectionId;

        public IJsonMessageWriter MessageWriter => _connectionContext.Writer;

        public IConnectionContext Connection => _connectionContext;

        public OperationMessage Op { get; }
    }
}

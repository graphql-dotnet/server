namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class MessageTypes
    {
        public const string GQL_CONNECTION_INIT = "connection_init"; // Client -> Server
        public const string GQL_CONNECTION_ACK = "connection_ack"; // Server -> Client
        public const string GQL_CONNECTION_ERROR = "connection_error"; // Server -> Client

        // NOTE: This one here don't follow the standard due to connection optimization
        public const string GQL_CONNECTION_KEEP_ALIVE = "ka"; // Server -> Client

        public const string GQL_CONNECTION_TERMINATE = "connection_terminate"; // Client -> Server
        public const string GQL_START = "start"; // Client -> Server
        public const string GQL_DATA = "data"; // Server -> Client
        public const string GQL_ERROR = "error"; // Server -> Client
        public const string GQL_COMPLETE = "complete"; // Server -> Client
        public const string GQL_STOP = "stop"; // Client -> Server
    }
}

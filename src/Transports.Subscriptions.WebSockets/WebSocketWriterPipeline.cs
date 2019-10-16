using GraphQL.Http;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketWriterPipeline : IWriterPipeline
    {
        private readonly WebSocket _socket;
        private readonly IDocumentWriter _documentWriter;
        private readonly ITargetBlock<OperationMessage> _startBlock;

        public WebSocketWriterPipeline(WebSocket socket, IDocumentWriter documentWriter)
        {
            _socket = socket;
            _documentWriter = documentWriter;

            _startBlock = CreateMessageWriter();
        }

        public bool Post(OperationMessage message)
        {
            return _startBlock.Post(message);
        }

        public Task SendAsync(OperationMessage message)
        {
            return _startBlock.SendAsync(message);
        }

        public Task Completion => _startBlock.Completion;

        public Task Complete()
        {
            _startBlock.Complete();
            return Task.CompletedTask;
        }

        private ITargetBlock<OperationMessage> CreateMessageWriter()
        {
            var target = new ActionBlock<OperationMessage>(
                WriteMessageAsync, new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1,
                    EnsureOrdered = true
                });

            return target;
        }

        private async Task WriteMessageAsync(OperationMessage message)
        {
            if (_socket.CloseStatus.HasValue) return;

            var stream = new WebsocketWriterStream(_socket);
            try
            {
                await _documentWriter.WriteAsync(stream, message)
                    .ConfigureAwait(false);
            }
            finally
            {
                await stream.FlushAsync().ConfigureAwait(false);
                stream.Dispose();
            }
        }
    }
}
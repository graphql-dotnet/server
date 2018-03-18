using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketWriterPipeline : IWriterPipeline
    {
        private readonly ITargetBlock<string> _endBlock;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly WebSocket _socket;
        private readonly IPropagatorBlock<OperationMessage, string> _startBlock;

        public WebSocketWriterPipeline(WebSocket socket, JsonSerializerSettings serializerSettings)
        {
            _socket = socket;
            _serializerSettings = serializerSettings;

            _endBlock = CreateMessageWriter();
            _startBlock = CreateWriterJsonTransformer();

            _startBlock.LinkTo(_endBlock, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
        }

        public bool Post(OperationMessage message)
        {
            return _startBlock.Post(message);
        }

        public Task SendAsync(OperationMessage message)
        {
            return _startBlock.SendAsync(message);
        }

        public Task Completion => _endBlock.Completion;

        public Task Complete()
        {
            _startBlock.Complete();
            return Task.CompletedTask;
        }

        protected IPropagatorBlock<OperationMessage, string> CreateWriterJsonTransformer()
        {
            var transformer = new TransformBlock<OperationMessage, string>(
                input => JsonConvert.SerializeObject(input, _serializerSettings),
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = true
                });

            return transformer;
        }

        private ITargetBlock<string> CreateMessageWriter()
        {
            var target = new ActionBlock<string>(
                WriteMessageAsync, new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1,
                    MaxDegreeOfParallelism = 1,
                    EnsureOrdered = true
                });

            return target;
        }

        private Task WriteMessageAsync(string message)
        {
            if (_socket.CloseStatus.HasValue) return Task.CompletedTask;

            var messageSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            return _socket.SendAsync(messageSegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
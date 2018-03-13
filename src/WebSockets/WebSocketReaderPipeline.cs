using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketReaderPipeline : IReaderPipeline
    {
        private readonly IPropagatorBlock<string, OperationMessage> _endBlock;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly WebSocket _socket;
        private readonly ISourceBlock<string> _startBlock;

        public WebSocketReaderPipeline(WebSocket socket, JsonSerializerSettings serializerSettings)
        {
            _socket = socket;
            _serializerSettings = serializerSettings;

            _startBlock = CreateMessageReader();
            _endBlock = CreateReaderJsonTransformer();
            _startBlock.LinkTo(_endBlock, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
        }

        public void LinkTo(ITargetBlock<OperationMessage> target)
        {
            _endBlock.LinkTo(target, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
        }

        public Task Complete()
        {
            _startBlock.Complete();
            return Task.CompletedTask;
        }

        public Task Completion => _endBlock.Completion;

        protected IPropagatorBlock<string, OperationMessage> CreateReaderJsonTransformer()
        {
            var transformer = new TransformBlock<string, OperationMessage>(
                input => JsonConvert.DeserializeObject<OperationMessage>(input, _serializerSettings),
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = true
                });

            return transformer;
        }

        protected ISourceBlock<string> CreateMessageReader()
        {
            IPropagatorBlock<string, string> source = new BufferBlock<string>(
                new ExecutionDataflowBlockOptions
                {
                    EnsureOrdered = true,
                    BoundedCapacity = 1,
                    MaxDegreeOfParallelism = 1
                });

            Task.Run(async () =>
            {
                while (!source.Completion.IsCompleted || !source.Completion.IsCanceled || !_socket.CloseStatus.HasValue)
                    await ReadMessageAsync(source);
            });

            return source;
        }

        private async Task ReadMessageAsync(ITargetBlock<string> target)
        {
            string message = null;
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);

            using (var memoryStream = new MemoryStream())
            {
                try
                {
                    WebSocketReceiveResult receiveResult;

                    do
                    {
                        receiveResult = await _socket.ReceiveAsync(segment, CancellationToken.None);

                        if (receiveResult.CloseStatus.HasValue)
                            target.Complete();

                        if (receiveResult.Count == 0)
                            continue;

                        await memoryStream.WriteAsync(segment.Array, segment.Offset, receiveResult.Count);
                    } while (!receiveResult.EndOfMessage || memoryStream.Length == 0);

                    message = Encoding.UTF8.GetString(memoryStream.ToArray());
                }
                catch (Exception x)
                {
                    target.Fault(x);
                }
            }

            await target.SendAsync(message);
        }
    }
}
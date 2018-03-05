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
    public class WebSocketClient : IMessageTransport
    {
        private readonly WebSocket _socket;

        public WebSocketClient(WebSocket socket)
        {
            _socket = socket;
            Reader = CreateReader();
            Writer = CreateWriter();
        }


        public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

        public ISourceBlock<OperationMessage> Reader { get; }

        public ITargetBlock<OperationMessage> Writer { get; }

        private ITargetBlock<OperationMessage> CreateWriter()
        {
        }

        private ISourceBlock<OperationMessage> CreateReader()
        {
            var messageReader = CreateMessageReader();
            var jsonTransformer = CreateReaderJsonTransformer();
            var buffer = new BufferBlock<OperationMessage>();

            messageReader.LinkTo(jsonTransformer, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
            jsonTransformer.LinkTo(buffer, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            return buffer;
        }

        protected IPropagatorBlock<string, OperationMessage> CreateReaderJsonTransformer()
        {
            var transformer = new TransformBlock<string, OperationMessage>(
                input => JsonConvert.DeserializeObject<OperationMessage>(input),
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
                while (!source.Completion.IsCompleted || !source.Completion.IsCanceled)
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

        public Task CloseAsync()
        {
            if (_socket.State != WebSocketState.Open)
                return Task.CompletedTask;

            if (CloseStatus.HasValue)
                if (CloseStatus != WebSocketCloseStatus.NormalClosure || CloseStatus != WebSocketCloseStatus.Empty)
                    return AbortAsync();

            return _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }

        private Task AbortAsync()
        {
            _socket.Abort();
            return Task.CompletedTask;
        }
    }
}
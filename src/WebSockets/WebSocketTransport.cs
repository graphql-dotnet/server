using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketTransport : IMessageTransport
    {
        private readonly WebSocket _socket;
        private ISourceBlock<string> _messageReader;
        private ITargetBlock<OperationMessage> _messageWriter;
        private readonly JsonSerializerSettings _serializerSettings;

        public WebSocketTransport(WebSocket socket)
        {
            _socket = socket;
            Reader = CreateReader();
            Writer = CreateWriter();
            _serializerSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }


        public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

        public ISourceBlock<OperationMessage> Reader { get; }

        public ITargetBlock<OperationMessage> Writer { get; }

        private ITargetBlock<OperationMessage> CreateWriter()
        {
            var messageWriter = CreateMessageWriter();

            var transformer = CreateWriterJsonTransformer();
            var buffer = new BufferBlock<OperationMessage>();

            buffer.LinkTo(transformer, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            transformer.LinkTo(messageWriter, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            _messageWriter = buffer;
            return buffer;
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
            if (CloseStatus.HasValue) return Task.CompletedTask;

            var messageSegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            return _socket.SendAsync(messageSegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private ISourceBlock<OperationMessage> CreateReader()
        {
            _messageReader = CreateMessageReader();
            var jsonTransformer = CreateReaderJsonTransformer();
            var buffer = new BufferBlock<OperationMessage>();

            _messageReader.LinkTo(jsonTransformer, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });
            jsonTransformer.LinkTo(buffer, new DataflowLinkOptions
            {
                PropagateCompletion = true
            });

            return buffer;
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
            _messageWriter.Complete();
            _messageReader.Complete();

            if (_socket.State != WebSocketState.Open)
                return Task.CompletedTask;

            if (CloseStatus.HasValue)
                if (CloseStatus != WebSocketCloseStatus.NormalClosure || CloseStatus != WebSocketCloseStatus.Empty)
                    return AbortAsync();

            return _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
        }

        private Task AbortAsync()
        {
            _messageWriter.Complete();
            _messageReader.Complete();
            _socket.Abort();
            return Task.CompletedTask;
        }
    }
}
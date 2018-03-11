using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class TestableReader : IReaderPipeline
    {
        private readonly BufferBlock<OperationMessage> _readBuffer;

        public TestableReader()
        {
            _readBuffer = new BufferBlock<OperationMessage>();
        }

        public void LinkTo(ITargetBlock<OperationMessage> target)
        {
            _readBuffer.LinkTo(target, new DataflowLinkOptions()
            {
                PropagateCompletion = true
            });
        }

        public Task Complete()
        {
           _readBuffer.Complete();
            return Task.CompletedTask;
        }

        public Task Completion => _readBuffer.Completion;

        public bool AddMessageToRead(OperationMessage message)
        {
            return _readBuffer.Post(message);
        }
    }

    public class TestableWriter : IWriterPipeline
    {
        private readonly ActionBlock<OperationMessage> _endBlock;

        public TestableWriter()
        {
            WrittenMessages = new List<OperationMessage>();
            _endBlock = new ActionBlock<OperationMessage>(message => { WrittenMessages.Add(message); });
        }

        public List<OperationMessage> WrittenMessages { get; }

        public bool Post(OperationMessage message)
        {
            return _endBlock.Post(message);
        }

        public Task SendAsync(OperationMessage message)
        {
            return _endBlock.SendAsync(message);
        }

        public Task Completion => _endBlock.Completion;

        public Task Complete()
        {
            _endBlock.Complete();
            return Task.CompletedTask;
        }
    }

    public class TestableSubscriptionTransport : IMessageTransport
    {
        public TestableSubscriptionTransport()
        {
            Writer = new TestableWriter();
            Reader = new TestableReader();
        }

        public IReaderPipeline Reader { get; }

        public IWriterPipeline Writer { get; }
    }
}
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class TestableSubscriptionTransport : IMessageTransport
    {
        private BufferBlock<OperationMessage> _readBuffer;

        public TestableSubscriptionTransport()
        {
            Writer = CreateWriter();
            Reader = CreateReader();
            WrittenMessages = new List<OperationMessage>();
        }

        public List<OperationMessage> WrittenMessages { get; }

        public ISourceBlock<OperationMessage> Reader { get; set; }

        public ITargetBlock<OperationMessage> Writer { get; set; }

        private ITargetBlock<OperationMessage> CreateWriter()
        {
            return new ActionBlock<OperationMessage>(message => { WrittenMessages.Add(message); });
        }

        private ISourceBlock<OperationMessage> CreateReader()
        {
            _readBuffer = new BufferBlock<OperationMessage>();
            return _readBuffer;
        }

        public bool AddMessageToRead(OperationMessage message)
        {
            return _readBuffer.Post(message);
        }

        public void Complete()
        {
            _readBuffer.Complete();
        }
    }
}
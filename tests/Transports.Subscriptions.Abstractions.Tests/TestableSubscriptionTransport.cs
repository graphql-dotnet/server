using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests
{
    public class TestableSubscriptionTransport : IMessageTransport
    {
        private readonly BufferBlock<OperationMessage> _readBuffer;

        public TestableSubscriptionTransport()
        {
            WrittenMessages = new List<OperationMessage>();
            _readBuffer = new BufferBlock<OperationMessage>();
        }

        public List<OperationMessage> WrittenMessages { get; }

        public Task CloseAsync()
        {
            _readBuffer.Complete();
            return Task.CompletedTask;
        }

        public ITargetBlock<OperationMessage> CreateWriter()
        {
            var handler = new ActionBlock<OperationMessage>(message => { WrittenMessages.Add(message); });
            return handler;
        }

        public ISourceBlock<OperationMessage> CreateReader()
        { 
            return _readBuffer;
        }

        public bool AddMessageToRead(OperationMessage message)
        {
            return _readBuffer.Post(message);
        }
    }
}
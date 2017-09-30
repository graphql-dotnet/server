using System;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class SubscriptionProtocolHandlerFacts
    {
        private readonly TestSchema _schema;
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ISubscriptionExecuter _subscriptionExecuter;
        private SubscriptionProtocolHandler<TestSchema> _sut;
        private IJsonMessageWriter _messageWriter;

        public SubscriptionProtocolHandlerFacts()
        {
            _schema = new TestSchema();
            _documentExecuter = Substitute.For<IDocumentExecuter>();
            _subscriptionExecuter = Substitute.For<ISubscriptionExecuter>();
            _messageWriter = Substitute.For<IJsonMessageWriter>();
            
            var logger = Substitute.For<ILogger<SubscriptionProtocolHandler<TestSchema>>>();
            _sut = new SubscriptionProtocolHandler<TestSchema>(
                _schema,
                _subscriptionExecuter,
                _documentExecuter,
                logger);
        }

        [Fact]
        public async Task should_handle_init()
        {
            /* Given */
            var messageContext = CreateMessage(
                MessageTypes.GQL_CONNECTION_INIT, null);

            /* When */
            await _sut.HandleMessageAsync(messageContext).ConfigureAwait(false);

            /* Then */
            await messageContext.MessageWriter
                .Received()
                .WriteMessageAsync(Arg.Is<OperationMessage>(
                    message => message.Type == MessageTypes.GQL_CONNECTION_ACK)).ConfigureAwait(false);
        }

        private OperationMessageContext CreateMessage(string type, object payload)
        {
            var op = new OperationMessage()
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Payload = payload != null ? JObject.FromObject(payload): null
            };

            return new OperationMessageContext("1", _messageWriter, op);
        }
    }
}

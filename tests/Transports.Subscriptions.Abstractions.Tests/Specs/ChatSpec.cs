using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Samples.Schemas.Chat;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests.Specs
{
    public class ChatSpec
    {
        public ChatSpec()
        {
            _chat = new Chat();
            _transport = new TestableSubscriptionTransport();
            _transportReader = _transport.Reader as TestableReader;
            _transportWriter = _transport.Writer as TestableWriter;
            _subscriptions = new SubscriptionManager(
                new DefaultSchemaExecuter<ChatSchema>(
                    new DocumentExecuter(),
                    new ChatSchema(_chat)),
                new NullLoggerFactory());

            _server = new SubscriptionServer(
                _transport,
                _subscriptions,
                new []{ new ProtocolMessageListener(new NullLogger<ProtocolMessageListener>())},
                new NullLogger<SubscriptionServer>()
            );
        }

        private readonly Chat _chat;
        private readonly TestableSubscriptionTransport _transport;
        private readonly SubscriptionManager _subscriptions;
        private readonly SubscriptionServer _server;
        private TestableReader _transportReader;
        private TestableWriter _transportWriter;

        private void AssertReceivedData(List<OperationMessage> writtenMessages, Predicate<JObject> predicate)
        {
            var dataMessages = writtenMessages.Where(m => m.Type == MessageType.GQL_DATA);
            var results = dataMessages.Select(m => m.Payload["data"] as JObject)
                .ToList();

            Assert.Contains(results, predicate);
        }

        [Fact]
        public async Task Mutate_messages()
        {
            /* Given */
            _chat.AddMessage(new ReceivedMessage
            {
                Content = "test",
                FromId = "1",
                SentAt = DateTime.Now
            });

            var id = "1";
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_INIT
            });
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_START,
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    OperationName = "",
                    Query = @"mutation AddMessage($message: MessageInputType!) {
  addMessage(message: $message) {
    from {
      id
      displayName
    }
    content
  }
}",
                    Variables = JObject.Parse(@"{
  ""message"": {
        ""content"": ""Message"",
        ""fromId"": ""1""
    }
}")
                })
            });
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_TERMINATE
            });

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_CONNECTION_ACK);
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);
        }

        [Fact]
        public async Task Query_messages()
        {
            /* Given */
            _chat.AddMessage(new ReceivedMessage
            {
                Content = "test",
                FromId = "1",
                SentAt = DateTime.Now
            });

            var id = "1";
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_INIT
            });
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_START,
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    OperationName = "",
                    Query = @"query AllMessages { 
    messages {
        content
        sentAt
        from {
            id
            displayName
        }
    }
}"
                })
            });
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_TERMINATE
            });

            /* When */
            await _server.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_CONNECTION_ACK);
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);
        }

        [Fact]
        public async Task Subscribe_and_mutate_messages()
        {
            /* Given */
            // subscribe
            var id = "1";
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_INIT
            });
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_START,
                Payload = JObject.FromObject(new OperationMessagePayload
                {
                    OperationName = "",
                    Query = @"subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
    sentAt
  }
}"
                })
            });

            // post message
            _chat.AddMessage(new ReceivedMessage
            {
                FromId = "1",
                Content = "content",
                SentAt = DateTime.Now
            });

            /* When */
            _transportReader.AddMessageToRead(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_TERMINATE
            });

            await _server.OnConnect();

            /* Then */
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_CONNECTION_ACK);
            AssertReceivedData(_transportWriter.WrittenMessages, data => data.ContainsKey("messageAdded"));
            Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);
        }
    }
}
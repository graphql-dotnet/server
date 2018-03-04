using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Samples.Schemas.Chat;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests.Specs
{
    public class ChatSpec
    {
        private readonly Chat _chat;
        private readonly TestableSubscriptionTransport _transport;
        private readonly SubscriptionManager _subscriptions;
        private SubscriptionServer _server;

        public ChatSpec()
        {
            _chat = new Chat();
            _transport = new TestableSubscriptionTransport();
            _subscriptions = new SubscriptionManager(
                new DefaultSchemaExecuter<ChatSchema>(
                    new DocumentExecuter(),
                    new ChatSchema(_chat)));
            _server = new SubscriptionServer(
                _transport,
                _subscriptions
                );
        }

        [Fact]
        public async Task Query_messages()
        {
            /* Given */
            _chat.AddMessage(new ReceivedMessage()
            {
                Content = "test",
                FromId = "1",
                SentAt = DateTime.Now
            });

            var id = "1";
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_INIT
            });
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_START,
                Payload = new OperationMessagePayload()
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
                }

            });
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_TERMINATE
            });

            /* When */
            await _server.ReceiveMessagesAsync();

            /* Then */
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_CONNECTION_ACK);
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);

        }

        [Fact]
        public async Task Mutate_messages()
        {
            /* Given */
            _chat.AddMessage(new ReceivedMessage()
            {
                Content = "test",
                FromId = "1",
                SentAt = DateTime.Now
            });

            var id = "1";
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_INIT
            });
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_START,
                Payload = new OperationMessagePayload()
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
                }

            });
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_TERMINATE
            });

            /* When */
            await _server.ReceiveMessagesAsync();

            /* Then */
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_CONNECTION_ACK);
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_DATA);
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);

        }

        [Fact]
        public async Task Subscribe_and_mutate_messages()
        {
            /* Given */
            // subscribe
            var id = "1";
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_INIT
            });
            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_START,
                Payload = new OperationMessagePayload()
                {
                    OperationName = "",
                    Query = @"subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
    sentAt
  }
}"
                }

            });

            while (!_subscriptions.Any())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            // post message
            _chat.AddMessage(new ReceivedMessage()
            {
                FromId = "1",
                Content = "content",
                SentAt = DateTime.Now
            });

            _chat.AddMessage(new ReceivedMessage()
            {
                FromId = "2",
                Content = "content",
                SentAt = DateTime.Now
            });
            /* When */


            _transport.AddMessageToRead(new OperationMessage()
            {
                Id = id,
                Type = MessageType.GQL_CONNECTION_TERMINATE
            });

            await _server.ReceiveMessagesAsync();

            /* Then */
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_CONNECTION_ACK);
            AssertReceivedData(_transport.WrittenMessages, data => data.ContainsKey("messageAdded"));
            Assert.Contains(_transport.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);

        }

        private void AssertReceivedData(List<OperationMessage> writtenMessages, Predicate<IDictionary<string, object>> predicate)
        {
            var dataMessages = writtenMessages.Where(m => m.Type == MessageType.GQL_DATA);
            var results = dataMessages.Select(m => m.Payload)
                .OfType<ExecutionResult>()
                .Select(r => r.Data as IDictionary<string,object>)
                .ToList();

            Assert.Contains(results, predicate);
        }
    }
}
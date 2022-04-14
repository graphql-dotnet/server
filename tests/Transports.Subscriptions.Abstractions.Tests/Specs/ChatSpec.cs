using GraphQL.NewtonsoftJson;
using GraphQL.Samples.Schemas.Chat;
using GraphQL.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions.Tests.Specs;

public class ChatSpec
{
    public ChatSpec()
    {
        _chat = new Chat();
        _transport = new TestableSubscriptionTransport();
        _transportReader = _transport.Reader as TestableReader;
        _transportWriter = _transport.Writer as TestableWriter;
        _subscriptions = new SubscriptionManager(
            new SchemaDocumentExecuter(new ChatSchema(_chat, new DefaultServiceProvider())),
            new NullLoggerFactory(),
            NoopServiceScopeFactory.Instance);

        _server = new SubscriptionServer(
            _transport,
            _subscriptions,
            new[] { new ProtocolMessageListener(new NullLogger<ProtocolMessageListener>(), new GraphQLSerializer()) },
            new NullLogger<SubscriptionServer>()
        );
    }

    private readonly Chat _chat;
    private readonly TestableSubscriptionTransport _transport;
    private readonly SubscriptionManager _subscriptions;
    private readonly SubscriptionServer _server;
    private readonly TestableReader _transportReader;
    private readonly TestableWriter _transportWriter;

    private void AssertReceivedData(List<OperationMessage> writtenMessages, Predicate<JObject> predicate)
    {
        var dataMessages = writtenMessages.Where(m => m.Type == MessageType.GQL_DATA);
        var results = dataMessages.Select(m =>
        {
            var executionResult = (ExecutionResult)m.Payload;
            return FromObject(executionResult);
        }).ToList();

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
            SentAt = DateTime.Now.Date
        });

        string id = "1";
        _transportReader.AddMessageToRead(new OperationMessage
        {
            Id = id,
            Type = MessageType.GQL_CONNECTION_INIT
        });
        _transportReader.AddMessageToRead(new OperationMessage
        {
            Id = id,
            Type = MessageType.GQL_START,
            Payload = FromObject(new GraphQLRequest
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
                Variables = ToInputs(@"{
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
            SentAt = DateTime.Now.Date
        });

        string id = "1";
        _transportReader.AddMessageToRead(new OperationMessage
        {
            Id = id,
            Type = MessageType.GQL_CONNECTION_INIT
        });
        _transportReader.AddMessageToRead(new OperationMessage
        {
            Id = id,
            Type = MessageType.GQL_START,
            Payload = FromObject(new GraphQLRequest
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
        string id = "1";
        _transportReader.AddMessageToRead(new OperationMessage
        {
            Id = id,
            Type = MessageType.GQL_CONNECTION_INIT
        });
        _transportReader.AddMessageToRead(new OperationMessage
        {
            Id = id,
            Type = MessageType.GQL_START,
            Payload = FromObject(new GraphQLRequest
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
            SentAt = DateTime.Now.Date
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
        AssertReceivedData(_transportWriter.WrittenMessages, data => ((JObject)data["data"]).ContainsKey("messageAdded"));
        Assert.Contains(_transportWriter.WrittenMessages, message => message.Type == MessageType.GQL_COMPLETE);
    }

    private Inputs ToInputs(string json)
        => new GraphQLSerializer().Deserialize<Inputs>(json);

    private JObject FromObject(object value)
    {
        var serializer = new GraphQLSerializer();
        var data = serializer.Serialize(value);
        return serializer.Deserialize<JObject>(data);
    }
}

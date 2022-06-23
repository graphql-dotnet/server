namespace Tests.WebSockets;

public class NewSubscriptionServerTests : IDisposable
{
    private readonly GraphQLHttpMiddlewareOptions _options = new();
    private readonly Mock<IWebSocketConnection> _mockStream = new(MockBehavior.Strict);
    private readonly IWebSocketConnection _stream;
    private readonly Mock<TestNewSubscriptionServer> _mockServer;
    private TestNewSubscriptionServer _server => _mockServer.Object;
    private readonly Mock<IDocumentExecuter> _mockDocumentExecuter = new(MockBehavior.Strict);
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory = new(MockBehavior.Strict);
    private readonly Mock<IGraphQLSerializer> _mockSerializer = new(MockBehavior.Strict);
    private readonly Mock<IUserContextBuilder> _mockUserContextBuilder = new(MockBehavior.Strict);

    public NewSubscriptionServerTests()
    {
        _mockStream.Setup(x => x.RequestAborted).Returns(default(CancellationToken));
        _stream = _mockStream.Object;
        _mockServer = new(_stream, _options, _mockDocumentExecuter.Object, _mockSerializer.Object,
            _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object);
        _mockServer.CallBase = true;
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public void Props()
    {
        _server.Get_DocumentExecuter.ShouldBe(_mockDocumentExecuter.Object);
        _server.Get_Serializer.ShouldBe(_mockSerializer.Object);
        _server.Get_ServiceScopeFactory.ShouldBe(_mockServiceScopeFactory.Object);
        _server.Get_UserContextBuilder.ShouldBe(_mockUserContextBuilder.Object);
    }

    [Fact]
    public void InvalidConstructorArgumentsThrows()
    {
        Should.Throw<ArgumentNullException>(() => new TestNewSubscriptionServer(_stream, _options,
            null!, _mockSerializer.Object, _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object));
        Should.Throw<ArgumentNullException>(() => new TestNewSubscriptionServer(_stream, _options,
            _mockDocumentExecuter.Object, null!, _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object));
        Should.Throw<ArgumentNullException>(() => new TestNewSubscriptionServer(_stream, _options,
            _mockDocumentExecuter.Object, _mockSerializer.Object, null!, _mockUserContextBuilder.Object));
        Should.Throw<ArgumentNullException>(() => new TestNewSubscriptionServer(_stream, _options,
            _mockDocumentExecuter.Object, _mockSerializer.Object, _mockServiceScopeFactory.Object, null!));
        _ = new TestNewSubscriptionServer(_stream, _options, _mockDocumentExecuter.Object,
            _mockSerializer.Object, _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Message_Initialize(bool initialized)
    {
        var message = new OperationMessage
        {
            Type = "connection_init",
        };
        if (initialized)
        {
            _server.Do_TryInitialize();
            _mockServer.Protected().Setup<Task>("ErrorTooManyInitializationRequestsAsync", message)
                .Returns(Task.CompletedTask).Verifiable();
        }
        else
        {
            _mockServer.Protected().Setup<Task>("OnConnectionInitAsync", message, true)
                .Returns(Task.CompletedTask).Verifiable();
        }
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("dummy")]
    [InlineData("start")]
    [InlineData("stop")]
    [InlineData("ka")]
    [InlineData("subscribe")]
    [InlineData("complete")]
    public async Task Message_ThrowsWhenNotInitialized(string? messageType)
    {
        var message = new OperationMessage
        {
            Type = messageType,
        };
        _mockServer.Protected().Setup<Task>("ErrorNotInitializedAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Message_Ping(bool initialized)
    {
        var message = new OperationMessage { Type = "ping" };
        _mockServer.Protected().Setup<Task>("OnPingAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        if (initialized)
        {
            _server.Do_TryInitialize();
        }
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Message_Pong(bool initialized)
    {
        var message = new OperationMessage { Type = "pong" };
        _mockServer.Protected().Setup<Task>("OnPongAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        if (initialized)
        {
            _server.Do_TryInitialize();
        }
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Message_Subscribe()
    {
        var message = new OperationMessage { Type = "subscribe" };
        _mockServer.Protected().Setup<Task>("OnSubscribeAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        _server.Do_TryInitialize();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Message_Complete()
    {
        var message = new OperationMessage { Type = "complete" };
        _mockServer.Protected().Setup<Task>("OnCompleteAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        _server.Do_TryInitialize();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ka")]
    [InlineData("dummy")]
    [InlineData("start")]
    [InlineData("stop")]
    [InlineData("connection_terminate")]
    public async Task Message_Unknown(string? messageType)
    {
        var message = new OperationMessage { Type = messageType };
        _mockServer.Protected().Setup<Task>("ErrorUnrecognizedMessageAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        _server.Do_TryInitialize();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnSendKeepAliveAsync()
    {
        _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
            .Returns<OperationMessage>(o => o.Type == "pong" ? Task.CompletedTask : Task.FromException(new Exception()))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("OnSendKeepAliveAsync").CallBase().Verifiable();
        await _server.Do_OnSendKeepAliveAsync();
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnConnectionAcknowledgeAsync()
    {
        _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
            .Returns<OperationMessage>(o => o.Type == "connection_ack" ? Task.CompletedTask : Task.FromException(new Exception()))
            .Verifiable();
        var message = new OperationMessage();
        _mockServer.Protected().Setup<Task>("OnConnectionAcknowledgeAsync", message).CallBase().Verifiable();
        await _server.Do_OnConnectionAcknowledgeAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPingAsync()
    {
        _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
            .Returns<OperationMessage>(o => o.Type == "pong" ? Task.CompletedTask : Task.FromException(new Exception()))
            .Verifiable();
        var message = new OperationMessage();
        _mockServer.Protected().Setup<Task>("OnPingAsync", message).CallBase().Verifiable();
        await _server.Do_OnPingAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OnPongAsync()
    {
        var message = new OperationMessage();
        _mockServer.Protected().Setup<Task>("OnPongAsync", message).CallBase().Verifiable();
        await _server.Do_OnPongAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task OnSubscribe(string id)
    {
        var message = new OperationMessage() { Id = id };
        _mockServer.Protected().Setup<Task>("OnSubscribeAsync", message).CallBase().Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false)
            .Returns(Task.CompletedTask)
            .Verifiable();
        await _server.Do_OnSubscribe(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task OnComplete(string id)
    {
        var message = new OperationMessage() { Id = id };
        _mockServer.Protected().Setup<Task>("OnCompleteAsync", message).CallBase().Verifiable();
        _mockServer.Protected().Setup<Task>("UnsubscribeAsync", message.Id == null ? ItExpr.IsNull<string>() : message.Id)
            .Returns(Task.CompletedTask)
            .Verifiable();
        await _server.Do_OnComplete(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task SendErrorResultAsync(bool wasSubscribed, bool hasErrorList)
    {
        var result = new ExecutionResult()
        {
            Errors = hasErrorList ? new ExecutionErrors { } : null,
        };
        if (wasSubscribed)
        {
            _server.Get_Subscriptions.TryAdd("abc", Mock.Of<IDisposable>());
            _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
                .Returns<OperationMessage>(o =>
                {
                    o.Id.ShouldBe("abc");
                    o.Type.ShouldBe("error");
                    o.Payload.ShouldBe(hasErrorList ? result.Errors : Array.Empty<ExecutionError>());
                    return Task.CompletedTask;
                })
                .Verifiable();
        }
        _mockServer.Protected().Setup("SendErrorResultAsync", "abc", ItExpr.IsAny<ExecutionResult>())
            .CallBase().Verifiable();
        await _server.Do_SendErrorResultAsync("abc", result);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendDataAsync(bool wasSubscribed)
    {
        var result = new ExecutionResult();
        if (wasSubscribed)
        {
            _server.Get_Subscriptions.TryAdd("abc", Mock.Of<IDisposable>());
            _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
                .Returns<OperationMessage>(o =>
                {
                    o.Id.ShouldBe("abc");
                    o.Type.ShouldBe("next");
                    o.Payload.ShouldBe(result);
                    return Task.CompletedTask;
                })
                .Verifiable();
        }
        _mockServer.Protected().Setup("SendDataAsync", "abc", ItExpr.IsAny<ExecutionResult>())
            .CallBase().Verifiable();
        await _server.Do_SendDataAsync("abc", result);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendCompletedAsync(bool wasSubscribed)
    {
        var result = new ExecutionResult();
        if (wasSubscribed)
        {
            _server.Get_Subscriptions.TryAdd("abc", Mock.Of<IDisposable>());
            _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
                .Returns<OperationMessage>(o =>
                {
                    o.Id.ShouldBe("abc");
                    o.Type.ShouldBe("complete");
                    o.Payload.ShouldBeNull();
                    return Task.CompletedTask;
                })
                .Verifiable();
        }
        _mockServer.Protected().Setup("SendCompletedAsync", "abc")
            .CallBase().Verifiable();
        await _server.Do_SendCompletedAsync("abc");
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteRequestAsync()
    {
        var payload = new object();
        var message = new OperationMessage
        {
            Payload = payload
        };
        var request = new GraphQLRequest
        {
            Query = "abc",
            Variables = new Inputs(new Dictionary<string, object?>()),
            Extensions = new Inputs(new Dictionary<string, object?>()),
            OperationName = "def",
        };
        _mockSerializer.Setup(x => x.ReadNode<GraphQLRequest>(payload))
            .Returns(request)
            .Verifiable();
        var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        var mockScope = new Mock<IServiceScope>(MockBehavior.Strict);
        mockScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(mockScope.Object)
            .Verifiable();
        mockScope.Setup(x => x.Dispose()).Verifiable();
        var result = Mock.Of<ExecutionResult>(MockBehavior.Strict);
        var mockUserContext = new Mock<IDictionary<string, object?>>(MockBehavior.Strict);
        _server.Set_UserContext(mockUserContext.Object);
        _mockDocumentExecuter.Setup(x => x.ExecuteAsync(It.IsAny<ExecutionOptions>()))
            .Returns<ExecutionOptions>(options =>
            {
                options.ShouldNotBeNull();
                options.Query.ShouldBe(request.Query);
                options.Variables.ShouldBe(request.Variables);
                options.Extensions.ShouldBe(request.Extensions);
                options.OperationName.ShouldBe(request.OperationName);
                options.UserContext.ShouldBe(mockUserContext.Object);
                options.RequestServices.ShouldBe(mockServiceProvider.Object);
                return Task.FromResult(result);
            })
            .Verifiable();
        var actual = await _server.Do_ExecuteRequestAsync(message);
        actual.ShouldBe(result);
        _mockDocumentExecuter.Verify();
        _mockSerializer.Verify();
        _mockServiceScopeFactory.Verify();
        _mockUserContextBuilder.Verify();
        mockServiceProvider.Verify();
        mockScope.Verify();
    }

    [Fact]
    public async Task ExecuteRequestAsync_Null()
    {
        var message = new OperationMessage();
        _mockSerializer.Setup(x => x.ReadNode<GraphQLRequest>(null))
            .Returns((GraphQLRequest?)null)
            .Verifiable();
        var result = Mock.Of<ExecutionResult>(MockBehavior.Strict);
        var mockUserContext = new Mock<IDictionary<string, object?>>(MockBehavior.Strict);
        _server.Set_UserContext(mockUserContext.Object);
        await Should.ThrowAsync<ArgumentNullException>(() => _server.Do_ExecuteRequestAsync(message));
        _mockDocumentExecuter.Verify();
        _mockSerializer.Verify();
        _mockServiceScopeFactory.Verify();
        _mockUserContextBuilder.Verify();
    }
}

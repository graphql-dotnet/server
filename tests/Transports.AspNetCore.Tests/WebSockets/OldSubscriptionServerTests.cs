using System.Security.Claims;

namespace Tests.WebSockets;

public class OldSubscriptionServerTests : IDisposable
{
    private readonly GraphQLHttpMiddlewareOptions _options = new();
    private readonly Mock<IWebSocketConnection> _mockStream = new(MockBehavior.Strict);
    private readonly IWebSocketConnection _stream;
    private readonly Mock<TestOldSubscriptionServer> _mockServer;
    private TestOldSubscriptionServer _server => _mockServer.Object;
    private readonly Mock<IDocumentExecuter> _mockDocumentExecuter = new(MockBehavior.Strict);
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory = new(MockBehavior.Strict);
    private readonly Mock<IGraphQLSerializer> _mockSerializer = new(MockBehavior.Strict);
    private readonly Mock<IUserContextBuilder> _mockUserContextBuilder = new(MockBehavior.Strict);

    public OldSubscriptionServerTests()
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
        Should.Throw<ArgumentNullException>(() => new TestOldSubscriptionServer(_stream, _options,
            null!, _mockSerializer.Object, _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object));
        Should.Throw<ArgumentNullException>(() => new TestOldSubscriptionServer(_stream, _options,
            _mockDocumentExecuter.Object, null!, _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object));
        Should.Throw<ArgumentNullException>(() => new TestOldSubscriptionServer(_stream, _options,
            _mockDocumentExecuter.Object, _mockSerializer.Object, null!, _mockUserContextBuilder.Object));
        Should.Throw<ArgumentNullException>(() => new TestOldSubscriptionServer(_stream, _options,
            _mockDocumentExecuter.Object, _mockSerializer.Object, _mockServiceScopeFactory.Object, null!));
        _ = new TestOldSubscriptionServer(_stream, _options, _mockDocumentExecuter.Object,
            _mockSerializer.Object, _mockServiceScopeFactory.Object, _mockUserContextBuilder.Object);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Message_Terminate(bool initialized)
    {
        var message = new OperationMessage
        {
            Type = "connection_terminate",
        };
        if (initialized)
        {
            _server.Do_TryInitialize();
        }
        _mockServer.Protected().Setup<Task>("OnCloseConnectionAsync").Returns(Task.CompletedTask).Verifiable();
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
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
            _mockServer.Protected().Setup<Task>("OnConnectionInitAsync", message, false)
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

    [Fact]
    public async Task Message_Start()
    {
        var message = new OperationMessage { Type = "start" };
        _mockServer.Protected().Setup<Task>("OnStartAsync", message)
            .Returns(Task.CompletedTask).Verifiable();
        _mockServer.Setup(x => x.OnMessageReceivedAsync(message)).CallBase().Verifiable();
        _server.Do_TryInitialize();
        await _server.OnMessageReceivedAsync(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Message_Stop()
    {
        var message = new OperationMessage { Type = "stop" };
        _mockServer.Protected().Setup<Task>("OnStopAsync", message)
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
    [InlineData("subscribe")]
    [InlineData("complete")]
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
            .Returns<OperationMessage>(o => o.Type == "ka" ? Task.CompletedTask : Task.FromException(new Exception()))
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

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task OnStart(string id)
    {
        var message = new OperationMessage() { Id = id };
        _mockServer.Protected().Setup<Task>("OnStartAsync", message).CallBase().Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, true)
            .Returns(Task.CompletedTask)
            .Verifiable();
        await _server.Do_OnStart(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task OnStop(string id)
    {
        var message = new OperationMessage() { Id = id };
        _mockServer.Protected().Setup<Task>("OnStopAsync", message).CallBase().Verifiable();
        _mockServer.Protected().Setup<Task>("UnsubscribeAsync", message.Id == null ? ItExpr.IsNull<string>() : message.Id)
            .Returns(Task.CompletedTask)
            .Verifiable();
        await _server.Do_OnStop(message);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendErrorResultAsync(bool wasSubscribed)
    {
        var result = new ExecutionResult();
        if (wasSubscribed)
        {
            _server.Get_Subscriptions.TryAdd("abc", Mock.Of<IDisposable>());
            _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>()))
                .Returns<OperationMessage>(o =>
                {
                    o.Id.ShouldBe("abc");
                    o.Type.ShouldBe("error");
                    o.Payload.ShouldBe(result);
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
                    o.Type.ShouldBe("data");
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
        var principal = new ClaimsPrincipal();
        var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
        mockContext.Setup(x => x.User).Returns(principal).Verifiable();
        _mockStream.Setup(x => x.HttpContext).Returns(mockContext.Object).Verifiable();
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
                options.User.ShouldBe(principal);
                return Task.FromResult(result);
            })
            .Verifiable();
        var actual = await _server.Do_ExecuteRequestAsync(message);
        actual.ShouldBe(result);
        mockContext.Verify();
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

    [Fact]
    public async Task ErrorAccessDeniedAsync()
    {
        _mockStream.Setup(x => x.SendMessageAsync(It.IsAny<OperationMessage>())).Returns<OperationMessage>(msg =>
        {
            msg.Type.ShouldBe("connection_error");
            msg.Payload.ShouldBe("Access denied");
            return Task.CompletedTask;
        }).Verifiable();
        _mockStream.Setup(x => x.CloseAsync(4401, "Access denied")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorAccessDeniedAsync();
        _mockStream.Verify();
    }
}

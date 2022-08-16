using System.Reactive.Subjects;
using System.Security.Claims;
using GraphQL.Execution;
using Microsoft.AspNetCore.Authorization;

namespace Tests.WebSockets;

public class BaseSubscriptionServerTests : IDisposable
{
    private readonly GraphQLHttpMiddlewareOptions _options = new();
    private readonly Mock<IWebSocketConnection> _mockStream = new(MockBehavior.Strict);
    private readonly IWebSocketConnection _stream;
    private readonly Mock<TestBaseSubscriptionServer> _mockServer;
    private TestBaseSubscriptionServer _server => _mockServer.Object;

    public BaseSubscriptionServerTests()
    {
        _mockStream.Setup(x => x.RequestAborted).Returns(default(CancellationToken));
        _stream = _mockStream.Object;
        _mockServer = new(_stream, _options);
        _mockServer.CallBase = true;
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public void InvalidConstructorArgumentsThrows()
    {
        var options = new GraphQLHttpMiddlewareOptions();
        Should.Throw<ArgumentNullException>(() => new TestBaseSubscriptionServer(null!, new GraphQLWebSocketOptions(), new GraphQLHttpMiddlewareOptions()));
        Should.Throw<ArgumentNullException>(() => new TestBaseSubscriptionServer(_stream, null!, new GraphQLHttpMiddlewareOptions()));
        Should.Throw<ArgumentNullException>(() => new TestBaseSubscriptionServer(_stream, new GraphQLWebSocketOptions(), null!));

        options = new();
        options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.FromSeconds(-1);
        Should.Throw<ArgumentOutOfRangeException>(() => new TestBaseSubscriptionServer(_stream, options));

        options = new();
        options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.FromMilliseconds(int.MaxValue + 100d);
        Should.Throw<ArgumentOutOfRangeException>(() => new TestBaseSubscriptionServer(_stream, options));

        options = new();
        options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.Zero;
        Should.Throw<ArgumentOutOfRangeException>(() => new TestBaseSubscriptionServer(_stream, options));

        options = new();
        options.WebSockets.ConnectionInitWaitTimeout = Timeout.InfiniteTimeSpan;
        _ = new TestBaseSubscriptionServer(_stream, options);

        options = new();
        options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.FromSeconds(1);
        _ = new TestBaseSubscriptionServer(_stream, options);

        options = new();
        options.WebSockets.KeepAliveTimeout = TimeSpan.FromSeconds(-1);
        Should.Throw<ArgumentOutOfRangeException>(() => new TestBaseSubscriptionServer(_stream, options));

        options = new();
        options.WebSockets.KeepAliveTimeout = TimeSpan.FromMilliseconds(int.MaxValue + 100d);
        Should.Throw<ArgumentOutOfRangeException>(() => new TestBaseSubscriptionServer(_stream, options));

        options = new();
        options.WebSockets.KeepAliveTimeout = TimeSpan.Zero;
        Should.Throw<ArgumentOutOfRangeException>(() => new TestBaseSubscriptionServer(_stream, options));

        options = new();
        options.WebSockets.KeepAliveTimeout = Timeout.InfiniteTimeSpan;
        _ = new TestBaseSubscriptionServer(_stream, options);

        options = new();
        options.WebSockets.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        _ = new TestBaseSubscriptionServer(_stream, options);
    }

    [Fact]
    public void Initialize_Works()
    {
        _server.Get_Initialized.ShouldBeFalse();
        _server.Get_Initialized.ShouldBeFalse();
        _server.Do_TryInitialize().ShouldBeTrue();
        _server.Get_Initialized.ShouldBeTrue();
        _server.Do_TryInitialize().ShouldBeFalse();
        _server.Get_Initialized.ShouldBeTrue();
    }

    [Fact]
    public async Task OnCloseConnection()
    {
        _mockStream.Setup(x => x.CloseAsync()).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_OnCloseConnectionAsync();
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorConnectionInitializationTimeoutAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4408, "Connection initialization timeout")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorConnectionInitializationTimeoutAsync();
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorTooManyInitializationRequestsAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4429, "Too many initialization requests")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorTooManyInitializationRequestsAsync(new OperationMessage());
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorNotInitializedAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4401, "Unauthorized")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorNotInitializedAsync(new OperationMessage());
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorUnrecognizedMessageAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4400, "Unrecognized message")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorUnrecognizedMessageAsync(new OperationMessage());
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorIdCannotBeBlankAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4400, "Id cannot be blank")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorIdCannotBeBlankAsync(new OperationMessage());
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorIdAlreadyExistsAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4409, "Subscriber for abc already exists")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorIdAlreadyExistsAsync(new OperationMessage { Id = "abc" });
        _mockStream.Verify();
    }

    [Fact]
    public async Task ErrorAccessDeniedAsync()
    {
        _mockStream.Setup(x => x.CloseAsync(4401, "Access denied")).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_ErrorAccessDeniedAsync();
        _mockStream.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnConnectionInitAsync_Infinite(bool smart)
    {
        _server.Get_Initialized.ShouldBeFalse();
        var msg = new OperationMessage();
        _mockServer.Protected().Setup<ValueTask<bool>>("AuthorizeAsync", msg).Returns(new ValueTask<bool>(true));
        _mockServer.Protected().SetupGet<TimeSpan>("DefaultKeepAliveTimeout").Returns(Timeout.InfiniteTimeSpan).Verifiable();
        _server.Get_DefaultKeepAliveTimeout.ShouldBe(Timeout.InfiniteTimeSpan);
        _mockServer.Protected().Setup<Task>("OnConnectionAcknowledgeAsync", msg).Returns(Task.CompletedTask).Verifiable();
        await _server.Do_OnConnectionInitAsync(msg, smart);
        _mockServer.Verify();
        _server.Get_Initialized.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnConnectionInitAsync_Short(bool smart)
    {
        _server.Get_Initialized.ShouldBeFalse();
        var msg = new OperationMessage();
        _mockServer.Protected().Setup<ValueTask<bool>>("AuthorizeAsync", msg).Returns(new ValueTask<bool>(true));
        var tcs = new TaskCompletionSource<bool>();
        _mockStream.Setup(x => x.LastMessageSentAt).Returns(DateTime.UtcNow);
        _mockServer.Protected().Setup<Task>("OnConnectionAcknowledgeAsync", msg).Returns(Task.CompletedTask).Verifiable();
        _mockServer.Protected().Setup<Task>("OnSendKeepAliveAsync").Returns(() =>
        {
            tcs.TrySetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        _options.WebSockets.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        await _server.Do_OnConnectionInitAsync(msg, smart);
        await tcs.Task;
        _mockServer.Verify();
        _server.Get_Initialized.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnConnectionInitAsync_FailValidation(bool smart)
    {
        _server.Get_Initialized.ShouldBeFalse();
        var msg = new OperationMessage();
        _mockServer.Protected().Setup<Task>("OnConnectionInitAsync", msg, smart).CallBase().Verifiable();
        _mockServer.Protected().Setup<ValueTask<bool>>("AuthorizeAsync", msg).Returns(new ValueTask<bool>(false)).Verifiable();
        await _server.Do_OnConnectionInitAsync(msg, smart);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
        _server.Get_Initialized.ShouldBeFalse();
    }

    [Fact]
    public async Task OnConnectionInitWaitTimeoutAsync()
    {
        _mockServer.Protected().Setup<Task>("OnConnectionInitWaitTimeoutAsync").CallBase().Verifiable();
        _mockServer.Protected().Setup<Task>("ErrorConnectionInitializationTimeoutAsync").Returns(Task.CompletedTask).Verifiable();
        await _server.Do_OnConnectionInitWaitTimeoutAsync();
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartConnectionInitTimer_Infinite()
    {
        _options.WebSockets.ConnectionInitWaitTimeout = Timeout.InfiniteTimeSpan;
        await _server.Do_InitializeConnectionAsync();
    }

    [Fact]
    public async Task StartConnectionInitTimer_Default()
    {
        _server.Get_DefaultConnectionTimeout.ShouldBe(TimeSpan.FromSeconds(10));
        await _server.Do_InitializeConnectionAsync();
    }

    [Fact]
    public async Task StartConnectionInitTimer_NotInitialized()
    {
        _options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.FromSeconds(1);
        var tcs = new TaskCompletionSource<bool>();
        _mockServer.Protected().Setup<Task>("OnConnectionInitWaitTimeoutAsync").Returns(() =>
        {
            tcs.TrySetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        await _server.Do_InitializeConnectionAsync();
        // verify that if it did not initialize, it triggers OnConnectionInitWaitTimeoutAsync
        (await Task.WhenAny(tcs.Task, Task.Delay(5000))).ShouldBe(tcs.Task);
        _mockServer.Verify();
    }

    [Fact]
    public async Task StartConnectionInitTimer_Initialized()
    {
        _options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.FromSeconds(1);
        var tcs = new TaskCompletionSource<bool>();
        _mockServer.Protected().Setup<Task>("OnConnectionInitWaitTimeoutAsync").Returns(() =>
        {
            tcs.TrySetResult(true);
            return Task.CompletedTask;
        });
        await _server.Do_InitializeConnectionAsync();
        _server.Do_TryInitialize();
        // verify that if it has initialized, it does not trigger OnConnectionInitWaitTimeoutAsync
        (await Task.WhenAny(tcs.Task, Task.Delay(5000))).ShouldNotBe(tcs.Task);
        _mockServer.Verify();
    }

    [Fact]
    public async Task StartConnectionInitTimer_Canceled()
    {
        _options.WebSockets.ConnectionInitWaitTimeout = TimeSpan.FromSeconds(1);
        var tcs = new TaskCompletionSource<bool>();
        _mockServer.Protected().Setup<Task>("OnConnectionInitWaitTimeoutAsync").Returns(() =>
        {
            tcs.TrySetResult(true);
            return Task.CompletedTask;
        });
        await _server.Do_InitializeConnectionAsync();
        _server.Dispose();
        // verify that after the server has disposed, it does not trigger OnConnectionInitWaitTimeoutAsync
        (await Task.WhenAny(tcs.Task, Task.Delay(5000))).ShouldNotBe(tcs.Task);
        _mockServer.Verify();
    }

    [Fact]
    public async Task HandleErrorDuringSubscribeAsync()
    {
        var ex = new InvalidOperationException("This is a test");
        var ex2 = await _server.Do_HandleErrorDuringSubscribeAsync(new OperationMessage(), ex);
        ex2.ShouldBeOfType<UnhandledError>();
        ex2.InnerException.ShouldBe(ex);
    }

    [Fact]
    public async Task HandleErrorFromSourceAsync()
    {
        var ex = new InvalidOperationException("This is a test");
        var ex2 = await _server.Do_HandleErrorFromSourceAsync(ex);
        ex2.ShouldBeOfType<UnhandledError>();
        ex2.InnerException.ShouldBe(ex);
    }

    [Fact]
    public async Task SendSingleResultAsync()
    {
        var testId = "abc";
        var message = new OperationMessage { Id = testId };
        var testResult = new ExecutionResult();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();
        _mockServer.Protected().Setup<Task>("SendDataAsync", testId, testResult).Returns(() =>
        {
            tcs1.TrySetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        _mockServer.Protected().Setup<Task>("SendCompletedAsync", testId).Returns(() =>
        {
            tcs2.TrySetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        await _server.Do_SendSingleResultAsync(message, testResult);
        await Task.WhenAll(tcs1.Task, tcs2.Task);
        _mockServer.Verify();
    }

    [Fact]
    public async Task SendErrorResultAsync()
    {
        var testId = "abc";
        var testError = new ExecutionError("testing");
        var tcs1 = new TaskCompletionSource<bool>();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", testId, ItExpr.IsAny<ExecutionResult>()).Returns<string, ExecutionResult>((_, actualResult) =>
        {
            actualResult.Errors.ShouldNotBeNull();
            actualResult.Errors.Single().ShouldBe(testError);
            tcs1.SetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", testId, testError).CallBase().Verifiable();
        await _server.Do_SendErrorResultAsync(testId, testError);
        await tcs1.Task;
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendErrorResultAsync_2()
    {
        var message = new OperationMessage { Id = "abc" };
        var error = new ExecutionError("test");
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message.Id, ItExpr.IsAny<ExecutionResult>()).Returns<string, ExecutionResult>((_, result) =>
        {
            result.ShouldNotBeNull();
            result.Errors.ShouldNotBeNull();
            result.Errors.ShouldHaveSingleItem().ShouldBe(error);
            return Task.CompletedTask;
        }).Verifiable();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message, error).CallBase().Verifiable();
        await _server.Do_SendErrorResultAsync(message, error);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendErrorResultAsync_3()
    {
        var message = new OperationMessage { Id = "abc" };
        var result = new ExecutionResult();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message.Id, result).Returns(Task.CompletedTask).Verifiable();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message, result).CallBase().Verifiable();
        await _server.Do_SendErrorResultAsync(message, result);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public async Task Subscribe_NullReturnsError(string? id, bool overwrite)
    {
        var tcs1 = new TaskCompletionSource<bool>();
        var message = new OperationMessage { Id = id };
        _mockServer.Protected().Setup<Task>("ErrorIdCannotBeBlankAsync", message).Returns(() =>
        {
            tcs1.SetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        await _server.Do_SubscribeAsync(message, overwrite);
        await tcs1.Task;
        _mockServer.Verify();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscribe_Works(bool overwrite)
    {
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, overwrite).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        await _server.Do_SubscribeAsync(message, overwrite);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscribe_WithExistingId(bool overwrite)
    {
        var message1 = new OperationMessage { Id = "abc" };
        var source1 = new Subject<ExecutionResult>();
        var result1 = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source1 },
                },
        };
        var message2 = new OperationMessage { Id = "abc" };
        var source2 = new Subject<ExecutionResult>();
        var result2 = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source2 },
                },
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message1)
            .Returns(Task.FromResult<ExecutionResult>(result1))
            .Verifiable();
        if (overwrite)
        {
            _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message2)
                .Returns(Task.FromResult<ExecutionResult>(result2))
                .Verifiable();
        }
        else
        {
            _mockServer.Protected().Setup<Task>("ErrorIdAlreadyExistsAsync", message2)
                .Returns(Task.CompletedTask)
                .Verifiable();
        }
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message1, overwrite).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message1).Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message2, overwrite).Verifiable();
        if (overwrite)
            _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message2).Verifiable();
        await _server.Do_SubscribeAsync(message1, overwrite);
        _server.Get_Subscriptions.Contains(message1.Id).ShouldBeTrue();
        await _server.Do_SubscribeAsync(message2, overwrite);
        _server.Get_Subscriptions.Contains(message2.Id).ShouldBeTrue();
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscribe_CompletedSingleResult(bool withData)
    {
        var message = new OperationMessage { Id = "abc" };
        var result = new ExecutionResult
        {
            Executed = true,
            Data = withData ? new object { } : null,
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendSingleResultAsync", message, result)
            .Returns(Task.CompletedTask)
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_NotCompletedResult()
    {
        var message = new OperationMessage { Id = "abc" };
        var result = new ExecutionResult
        {
            Executed = false,
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message, result)
            .Returns(Task.CompletedTask)
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_ThrowsWhenDisposed()
    {
        var message = new OperationMessage { Id = "abc" };
        var result = new ExecutionResult
        {
            Executed = false,
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(() =>
            {
                _server.Dispose();
                return Task.FromException<ExecutionResult>(new OperationCanceledException());
            })
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Setup(x => x.Dispose()).CallBase().Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_HandlesThrownExceptions()
    {
        var message = new OperationMessage { Id = "abc" };
        var ex = new InvalidOperationException();
        var ex2 = new UnhandledError("test", ex);
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromException<ExecutionResult>(ex))
            .Verifiable();
        _mockServer.Protected().Setup<Task<ExecutionError>>("HandleErrorDuringSubscribeAsync", message, ex)
            .Returns(Task.FromResult<ExecutionError>(ex2))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message, ex2)
            .Returns(Task.CompletedTask)
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_HandlesThrownExcecutionError()
    {
        var message = new OperationMessage { Id = "abc" };
        var ex = new ExecutionError("sample");
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromException<ExecutionResult>(ex))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendErrorResultAsync", message, ex)
            .Returns(Task.CompletedTask)
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_DoesNotSendWhenDisposed()
    {
        var message = new OperationMessage { Id = "abc" };
        var result = new ExecutionResult
        {
            Executed = false,
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(() =>
            {
                _server.Dispose();
                return Task.FromResult(result);
            })
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Setup(x => x.Dispose()).CallBase().Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_DoesNotSendWhenUnsubscribed()
    {
        var message = new OperationMessage { Id = "abc" };
        var result = new ExecutionResult
        {
            Executed = false,
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(() =>
            {
                _server.Do_UnsubscribeAsync(message.Id);
                return Task.FromResult(result);
            })
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("UnsubscribeAsync", message.Id).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_RunsAsynchronously()
    {
        var message1 = new OperationMessage { Id = "abc" };
        var message2 = new OperationMessage { Id = "def" };
        var result1 = new ExecutionResult
        {
            Executed = true,
        };
        var result2 = new ExecutionResult
        {
            Executed = true,
        };
        var waiter = new TaskCompletionSource<bool>();
        var done = new TaskCompletionSource<bool>();
        var done2 = new TaskCompletionSource<bool>();
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message1)
            .Returns(async () =>
            {
                await waiter.Task;
                await done2.Task;
                return result1;
            })
            .Verifiable();
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message2)
            .Returns(async () =>
            {
                await waiter.Task;
                return result2;
            })
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message1, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message2, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSingleResultAsync", message1, result1).Returns(() =>
        {
            done.SetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSingleResultAsync", message2, result2).Returns(() =>
        {
            done2.SetResult(true);
            return Task.CompletedTask;
        }).Verifiable();
        await _server.Do_SubscribeAsync(message1, false);
        await _server.Do_SubscribeAsync(message2, false);
        done.Task.IsCompleted.ShouldBeFalse();
        waiter.SetResult(true);
        await done.Task;
        await done2.Task;
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Unsubscribe_Works()
    {
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        _mockServer.Protected().Setup<Task>("UnsubscribeAsync", message.Id).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.HasObservers.ShouldBeTrue();
        await _server.Do_UnsubscribeAsync(message.Id);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeFalse();
        await _server.Do_UnsubscribeAsync(message.Id);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeFalse();
        source.HasObservers.ShouldBeFalse();
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Unsubscribe_IgnoresInvalid(string? id)
    {
        var message = new OperationMessage { Id = id };
        _mockServer.Protected().Setup<Task>("UnsubscribeAsync", message.Id == null ? ItExpr.IsNull<string?>() : message.Id).Verifiable();
        await _server.Do_UnsubscribeAsync(message.Id);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Dispose_Unsubscribes_All()
    {
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        _mockServer.Setup(x => x.Dispose()).CallBase().Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.HasObservers.ShouldBeTrue();
        _server.Dispose();
        source.HasObservers.ShouldBeFalse();
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_DataEvents_Work()
    {
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        var result2 = new ExecutionResult();
        var result3 = new ExecutionResult();
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendDataAsync", message.Id, result2).Returns(Task.CompletedTask).Verifiable();
        _mockServer.Protected().Setup<Task>("SendDataAsync", message.Id, result3).Returns(Task.CompletedTask).Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.HasObservers.ShouldBeTrue();
        source.OnNext(result2);
        source.OnNext(result3);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscribe_DataEvents_CloseIfErrors(bool closeAfterError)
    {
        _options.WebSockets.DisconnectAfterAnyError = closeAfterError;
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        var result2 = new ExecutionResult()
        {
            Errors = new ExecutionErrors {
                    new ExecutionError("test"),
                },
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendDataAsync", message.Id, result2).Returns(Task.CompletedTask).Verifiable();
        if (closeAfterError)
        {
            _mockServer.Protected().Setup<Task>("SendCompletedAsync", message.Id)
                .Returns(Task.CompletedTask)
                .Verifiable();
        }
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.HasObservers.ShouldBeTrue();
        source.OnNext(result2);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_DataEvents_CompletedWorks()
    {
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        var result2 = new ExecutionResult()
        {
            Executed = true,
            Data = new object(),
        };
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendDataAsync", message.Id, result2).Returns(Task.CompletedTask).Verifiable();
        _mockServer.Protected().Setup<Task>("SendCompletedAsync", message.Id)
            .Returns(Task.CompletedTask)
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.HasObservers.ShouldBeTrue();
        source.OnNext(result2);
        source.OnCompleted();
        source.HasObservers.ShouldBeFalse(); //note: Subject<T> always disconnects after OnCompleted
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscribe_DataEvents_ErrorsWork_Exception(bool closeAfterError)
    {
        _options.WebSockets.DisconnectAfterErrorEvent = closeAfterError;
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        var error = new InvalidOperationException();
        var executionError = new UnhandledError("test", error);
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendDataAsync", message.Id, ItExpr.IsAny<ExecutionResult>())
            .Returns<string, ExecutionResult>((_, actualResult) =>
            {
                actualResult.Errors.ShouldNotBeNull();
                actualResult.Errors.Single().ShouldBe(executionError);
                return Task.CompletedTask;
            }).Verifiable();
        _mockServer.Protected().Setup<Task<ExecutionError>>("HandleErrorFromSourceAsync", error)
            .Returns(Task.FromResult<ExecutionError>(executionError))
            .Verifiable();
        if (closeAfterError)
        {
            _mockServer.Protected().Setup<Task>("SendCompletedAsync", message.Id)
                .Returns(Task.CompletedTask)
                .Verifiable();
        }
        _mockServer.Protected().Setup<Task>("SubscribeAsync", message, false).Verifiable();
        _mockServer.Protected().Setup<Task>("SendSubscriptionSuccessfulAsync", message).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.HasObservers.ShouldBeTrue();
        source.OnError(error);
        source.HasObservers.ShouldBeFalse(); //Subject<T> will dispose after OnError even if not requested
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Subscribe_DataEvents_ErrorsWork_ExecutionError()
    {
        var message = new OperationMessage { Id = "abc" };
        var source = new Subject<ExecutionResult>();
        var result = new ExecutionResult
        {
            Streams = new Dictionary<string, IObservable<ExecutionResult>> {
                    { "field", source },
                },
        };
        var error = new InvalidOperationException();
        var executionError = new UnhandledError("test", error);
        _mockServer.Protected().Setup<Task<ExecutionResult>>("ExecuteRequestAsync", message)
            .Returns(Task.FromResult<ExecutionResult>(result))
            .Verifiable();
        _mockServer.Protected().Setup<Task>("SendDataAsync", message.Id, ItExpr.IsAny<ExecutionResult>())
            .Returns<string, ExecutionResult>((_, actualResult) =>
            {
                actualResult.Errors.ShouldNotBeNull();
                actualResult.Errors.Single().ShouldBe(executionError);
                return Task.CompletedTask;
            }).Verifiable();
        await _server.Do_SubscribeAsync(message, false);
        source.HasObservers.ShouldBeTrue();
        _server.Get_Subscriptions.Contains(message.Id).ShouldBeTrue();
        source.OnError(executionError);
        source.HasObservers.ShouldBeFalse();
        _mockServer.Verify();
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        _server.Dispose();
        _server.Dispose();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthorizeAsync(bool authorized)
    {
        var msg = new OperationMessage();
        _options.AuthorizationRequired = true;
        if (!authorized)
        {
            _mockServer.Protected().Setup<Task>("OnNotAuthenticatedAsync", msg).CallBase().Verifiable();
            _mockServer.Protected().Setup<Task>("ErrorAccessDeniedAsync").CallBase().Verifiable();
            _mockStream.Setup(x => x.CloseAsync(4401, "Access denied")).Returns(Task.CompletedTask);
        }
        var user = new ClaimsPrincipal(authorized ? new ClaimsIdentity("test") : new ClaimsIdentity());
        var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
        mockContext.Setup(x => x.User).Returns(user);
        _mockStream.Setup(x => x.HttpContext).Returns(mockContext.Object);
        _mockServer.Protected().Setup<ValueTask<bool>>("AuthorizeAsync", msg).CallBase().Verifiable();
        var ret = await _server.Do_AuthorizeAsync(msg);
        ret.ShouldBe(authorized);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthorizeAsync_Roles(bool authorized)
    {
        var msg = new OperationMessage();
        _options.AuthorizedRoles.Add("myRole");
        if (!authorized)
        {
            _mockServer.Protected().Setup<Task>("OnNotAuthorizedRoleAsync", msg).CallBase().Verifiable();
            _mockServer.Protected().Setup<Task>("ErrorAccessDeniedAsync").CallBase().Verifiable();
            _mockStream.Setup(x => x.CloseAsync(4401, "Access denied")).Returns(Task.CompletedTask);
        }
        var user = new ClaimsPrincipal(authorized ? new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Role, "myRole") }, "Bearer") : new ClaimsIdentity("Bearer"));
        var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
        mockContext.Setup(x => x.User).Returns(user);
        _mockStream.Setup(x => x.HttpContext).Returns(mockContext.Object);
        _mockServer.Protected().Setup<ValueTask<bool>>("AuthorizeAsync", msg).CallBase().Verifiable();
        var ret = await _server.Do_AuthorizeAsync(msg);
        ret.ShouldBe(authorized);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthorizeAsync_Policy(bool authorized)
    {
        var msg = new OperationMessage();
        _options.AuthorizedPolicy = "myPolicy";
        if (!authorized)
        {
            _mockServer.Protected().Setup<Task>("OnNotAuthorizedPolicyAsync", msg, ItExpr.IsAny<AuthorizationResult>()).CallBase().Verifiable();
            _mockServer.Protected().Setup<Task>("ErrorAccessDeniedAsync").CallBase().Verifiable();
            _mockStream.Setup(x => x.CloseAsync(4401, "Access denied")).Returns(Task.CompletedTask);
        }
        var user = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));
        var mockAuthorizationService = new Mock<IAuthorizationService>(MockBehavior.Strict);
        mockAuthorizationService.Setup(x => x.AuthorizeAsync(user, null, "myPolicy")).Returns(authorized ? Task.FromResult(AuthorizationResult.Success()) : Task.FromResult(AuthorizationResult.Failed()));
        var mockProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        mockProvider.Setup(x => x.GetService(typeof(IAuthorizationService))).Returns(mockAuthorizationService.Object);
        var mockContext = new Mock<HttpContext>(MockBehavior.Strict);
        mockContext.Setup(x => x.User).Returns(user);
        mockContext.Setup(x => x.RequestServices).Returns(mockProvider.Object);
        _mockStream.Setup(x => x.HttpContext).Returns(mockContext.Object);
        _mockServer.Protected().Setup<ValueTask<bool>>("AuthorizeAsync", msg).CallBase().Verifiable();
        var ret = await _server.Do_AuthorizeAsync(msg);
        ret.ShouldBe(authorized);
        _mockServer.Verify();
        _mockServer.VerifyNoOtherCalls();
    }

    //note: BaseSubscriptionServer.Observer isn't fully tested here
}

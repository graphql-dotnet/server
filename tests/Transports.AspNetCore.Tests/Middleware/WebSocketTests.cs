namespace Tests.Middleware;

public class WebSocketTests : IDisposable
{
    private TestServer _server = null!;

    private void Configure(Action<GraphQLHttpMiddlewareOptions>? configureOptions = null, Action<IServiceCollection>? configureServices = null)
    {
        configureOptions ??= _ => { };
        configureServices ??= _ => { };

        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddAutoSchema<Chat.Query>(s => s
                    .WithMutation<Chat.Mutation>()
                    .WithSubscription<Chat.Subscription>())
                .AddSchema<Schema2>()
                .AddSystemTextJson());
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
            configureServices(services);
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL("/graphql", configureOptions);
            app.UseGraphQL<Schema2>("/graphql2", configureOptions);
        });
        _server = new TestServer(hostBuilder);
    }

    private class Schema2 : Schema
    {
        public Schema2()
        {
            Query = new AutoRegisteringObjectGraphType<Query2>();
        }
    }

    private class Query2
    {
        public static string? Var(string? test) => test;

        public static string? Ext(IResolveFieldContext context)
            => context.InputExtensions.TryGetValue("test", out var value) ? value?.ToString() : null;
    }

    public void Dispose() => _server?.Dispose();

    private WebSocketClient BuildClient(string subProtocol = "graphql-ws")
    {
        var webSocketClient = _server.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
        {
            request.Headers["Sec-WebSocket-Protocol"] = subProtocol;
        };
        webSocketClient.SubProtocols.Add(subProtocol);
        return webSocketClient;
    }

    [Fact]
    public async Task NoConfiguredHandlers()
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddSchema<Schema2>()
                .AddSystemTextJson());
        });

        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL<TestMiddleware>("/graphql", new object[] { new string[] { } });
        });

        _server = new TestServer(hostBuilder);

        var webSocketClient = BuildClient();
        var error = await Should.ThrowAsync<InvalidOperationException>(() => webSocketClient.ConnectAsync(new Uri(_server.BaseAddress, "/graphql"), default));
        error.Message.ShouldBe("Incomplete handshake, status code: 400");
    }

    [Fact]
    public async Task UnsupportedHandler()
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddSchema<Schema2>()
                .AddSystemTextJson());
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL<TestMiddleware>("/graphql", new object[] { new string[] { "unsupported" } });
        });

        _server = new TestServer(hostBuilder);
        var webSocketClient = BuildClient();
        var error = await Should.ThrowAsync<InvalidOperationException>(() => webSocketClient.ConnectAsync(new Uri(_server.BaseAddress, "/graphql"), default));
        error.Message.ShouldBe("Incomplete handshake, status code: 400");
    }

    private class TestMiddleware : GraphQLHttpMiddleware
    {
        private readonly string[] _subprotocols;

        public TestMiddleware(RequestDelegate next, string[] subprotocols) : base(next, new GraphQLSerializer(), Mock.Of<IDocumentExecuter>(MockBehavior.Strict), Mock.Of<IServiceScopeFactory>(MockBehavior.Strict), new GraphQLHttpMiddlewareOptions(), Mock.Of<IHostApplicationLifetime>(MockBehavior.Strict))
        {
            _subprotocols = subprotocols;
        }

        protected override IEnumerable<string> SupportedWebSocketSubProtocols => _subprotocols;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AppShuttingDownReturns(bool beforeConnect)
    {
        var tuple = new Tuple<CancellationTokenSource, TaskCompletionSource<bool>>(new(), new());

        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddSchema<Schema2>()
                .AddSystemTextJson());
            services.AddSingleton(tuple);
        });

        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            var options = new GraphQLHttpMiddlewareOptions();
            options.WebSockets.ConnectionInitWaitTimeout = Timeout.InfiniteTimeSpan;
            app.UseGraphQL<TestMiddleware2>("/graphql", options);
        });

        _server = new TestServer(hostBuilder);

        var webSocketClient = BuildClient();
        if (beforeConnect)
        {
            tuple.Item1.Cancel();
            using var socket = await webSocketClient.ConnectAsync(new Uri(_server.BaseAddress, "/graphql"), default);
            await tuple.Item2.Task;
        }
        else
        {
            using var socket = await webSocketClient.ConnectAsync(new Uri(_server.BaseAddress, "/graphql"), default);
            await Task.WhenAny(Task.Delay(1000), tuple.Item2.Task);
            tuple.Item2.Task.IsCompleted.ShouldBeFalse();
            tuple.Item1.Cancel();
            await tuple.Item2.Task;
        }
    }

    private class TestMiddleware2 : GraphQLHttpMiddleware
    {
        private readonly Tuple<CancellationTokenSource, TaskCompletionSource<bool>> _tuple;

        public TestMiddleware2(RequestDelegate next, IGraphQLTextSerializer serializer, IDocumentExecuter<ISchema> documentExecuter, IServiceScopeFactory serviceScopeFactory, GraphQLHttpMiddlewareOptions options, Tuple<CancellationTokenSource, TaskCompletionSource<bool>> tuple)
            : this(next, serializer, documentExecuter, serviceScopeFactory, options, Mock.Of<IHostApplicationLifetime>(MockBehavior.Strict), tuple)
        {
        }

        private TestMiddleware2(RequestDelegate next, IGraphQLTextSerializer serializer, IDocumentExecuter<ISchema> documentExecuter, IServiceScopeFactory serviceScopeFactory, GraphQLHttpMiddlewareOptions options, IHostApplicationLifetime hostApplicationLifetime, Tuple<CancellationTokenSource, TaskCompletionSource<bool>> tuple)
            : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
        {
            Mock.Get(hostApplicationLifetime).Setup(x => x.ApplicationStopping).Returns(tuple.Item1.Token);
            _tuple = tuple;
        }

        protected override async Task HandleWebSocketAsync(HttpContext context, RequestDelegate next)
        {
            await base.HandleWebSocketAsync(context, next);
            // this test also verifies that OCE isn't thrown when the app stopping token is triggered
            // because otherwise this line wouldn't run
            _tuple.Item2.SetResult(true);
        }
    }

    [Fact]
    public async Task Disabled()
    {
        Configure(o => o.HandleWebSockets = false);

        var webSocketClient = BuildClient();
        var error = await Should.ThrowAsync<InvalidOperationException>(() => webSocketClient.ConnectAsync(new Uri(_server.BaseAddress, "/graphql"), default));
        error.Message.ShouldBe("Incomplete handshake, status code: 404");
    }
}

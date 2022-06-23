namespace Tests;

public class UserContextBuilderTests : IDisposable
{
    private TestServer _server = null!;
    private HttpClient _client = null!;

    private void Configure(Action<IGraphQLBuilder> configureBuilder)
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b =>
            {
                configureBuilder(b);
                b.AddAutoSchema<MyQuery>();
                b.AddSystemTextJson();
            });
            services.AddHttpContextAccessor();
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL("/graphql");
        });
        _server = new TestServer(hostBuilder);
        _client = _server.CreateClient();
    }

    public void Dispose() => _server?.Dispose();

    private class MyQuery
    {
        public static string? Test([FromUserContext] MyUserContext ctx) => ctx.Name;
    }

    [Fact]
    public void NullChecks()
    {
        Func<HttpContext, MyUserContext> func = null!;
        Should.Throw<ArgumentNullException>(() => new UserContextBuilder<MyUserContext>(func));
        Func<HttpContext, ValueTask<MyUserContext>> func2 = null!;
        Should.Throw<ArgumentNullException>(() => new UserContextBuilder<MyUserContext>(func2));
        Func<HttpContext, object?, MyUserContext> func3 = null!;
        Should.Throw<ArgumentNullException>(() => new UserContextBuilder<MyUserContext>(func3));
        Func<HttpContext, object?, ValueTask<MyUserContext>> func4 = null!;
        Should.Throw<ArgumentNullException>(() => new UserContextBuilder<MyUserContext>(func4));
    }

    [Fact]
    public async Task Sync_Works()
    {
        var context = Mock.Of<HttpContext>(MockBehavior.Strict);
        var userContext = new MyUserContext();
        var builder = new UserContextBuilder<MyUserContext>(context2 =>
        {
            context2.ShouldBe(context);
            return userContext;
        });
        (await builder.BuildUserContextAsync(context, null)).ShouldBe(userContext);
    }

    [Fact]
    public async Task Async_Works()
    {
        var context = Mock.Of<HttpContext>(MockBehavior.Strict);
        var userContext = new MyUserContext();
        var builder = new UserContextBuilder<MyUserContext>(context2 =>
        {
            context2.ShouldBe(context);
            return new ValueTask<MyUserContext>(userContext);
        });
        (await builder.BuildUserContextAsync(context, null)).ShouldBe(userContext);
    }

    [Fact]
    public async Task Sync_Payload_Works()
    {
        var context = Mock.Of<HttpContext>(MockBehavior.Strict);
        var userContext = new MyUserContext();
        var builder = new UserContextBuilder<MyUserContext>((context2, payload) =>
        {
            context2.ShouldBe(context);
            payload.ShouldBe("test");
            return userContext;
        });
        (await builder.BuildUserContextAsync(context, "test")).ShouldBe(userContext);
    }

    [Fact]
    public async Task Async_Payload_Works()
    {
        var context = Mock.Of<HttpContext>(MockBehavior.Strict);
        var userContext = new MyUserContext();
        var builder = new UserContextBuilder<MyUserContext>((context2, payload) =>
        {
            context2.ShouldBe(context);
            payload.ShouldBe("test");
            return new ValueTask<MyUserContext>(userContext);
        });
        (await builder.BuildUserContextAsync(context, "test")).ShouldBe(userContext);
    }

    private async Task Test(string name)
    {
        using var response = await _client.GetAsync("/graphql?query={test}");
        response.EnsureSuccessStatusCode();
        var actual = await response.Content.ReadAsStringAsync();
        actual.ShouldBe(@"{""data"":{""test"":""" + name + @"""}}");
    }

    private async Task TestDirect(string name)
    {
        var executer = _server.Host.Services.GetRequiredService<IDocumentExecuter<ISchema>>();
        var result = await executer.ExecuteAsync(new ExecutionOptions
        {
            Query = "{test}",
            RequestServices = _server.Host.Services,
        });
        var serializer = _server.Host.Services.GetRequiredService<IGraphQLTextSerializer>();
        var actual = serializer.Serialize(result);
        actual.ShouldBe(@"{""data"":{""test"":""" + name + @"""}}");
    }

    [Fact]
    public async Task Builder1()
    {
        Configure(b => b.AddUserContextBuilder(ctx => new MyUserContext { Name = "John Doe" }));
        await Test("John Doe");
    }

    [Fact]
    public async Task Builder1Direct()
    {
        Configure(b => b.AddUserContextBuilder(ctx => new MyUserContext { Name = "John Doe" }));
        await TestDirect("John Doe");
    }

    [Fact]
    public void Builder1Null()
    {
        Should.Throw<ArgumentNullException>(() => Configure(b => b.AddUserContextBuilder((Func<HttpContext, MyUserContext>)null!)));
    }

    [Fact]
    public async Task Builder2()
    {
        Configure(b => b.AddUserContextBuilder(ctx => Task.FromResult(new MyUserContext { Name = "John Doe" })));
        await Test("John Doe");
    }

    [Fact]
    public async Task Builder2Direct()
    {
        Configure(b => b.AddUserContextBuilder(ctx => Task.FromResult(new MyUserContext { Name = "John Doe" })));
        await TestDirect("John Doe");
    }

    [Fact]
    public void Builder2Null()
    {
        Should.Throw<ArgumentNullException>(() => Configure(b => b.AddUserContextBuilder((Func<HttpContext, Task<MyUserContext>>)null!)));
    }

    [Fact]
    public async Task Builder3()
    {
        Configure(b => b.AddUserContextBuilder<MyBuilder>());
        await Test("John Doe");
    }

    [Fact]
    public async Task Builder3Direct()
    {
        Configure(b => b.AddUserContextBuilder<MyBuilder>());
        await TestDirect("John Doe");
    }

    [Fact]
    public async Task Builder4()
    {
        Configure(b => b.AddUserContextBuilder((ctx, payload) => new MyUserContext { Name = "John Doe" }));
        await Test("John Doe");
    }

    [Fact]
    public async Task Builder4Direct()
    {
        Configure(b => b.AddUserContextBuilder((ctx, payload) => new MyUserContext { Name = "John Doe" }));
        await TestDirect("John Doe");
    }

    [Fact]
    public void Builder4Null()
    {
        Should.Throw<ArgumentNullException>(() => Configure(b => b.AddUserContextBuilder((Func<HttpContext, object?, MyUserContext>)null!)));
    }

    [Fact]
    public async Task Builder5()
    {
        Configure(b => b.AddUserContextBuilder((ctx, payload) => Task.FromResult(new MyUserContext { Name = "John Doe" })));
        await Test("John Doe");
    }

    [Fact]
    public async Task Builder5Direct()
    {
        Configure(b => b.AddUserContextBuilder((ctx, payload) => Task.FromResult(new MyUserContext { Name = "John Doe" })));
        await TestDirect("John Doe");
    }

    [Fact]
    public void Builder5Null()
    {
        Should.Throw<ArgumentNullException>(() => Configure(b => b.AddUserContextBuilder((Func<HttpContext, object?, Task<MyUserContext>>)null!)));
    }

    private class MyBuilder : IUserContextBuilder
    {
        public ValueTask<IDictionary<string, object?>> BuildUserContextAsync(HttpContext context, object? payload)
            => new(new MyUserContext { Name = "John Doe" });
    }

    private class MyUserContext : Dictionary<string, object?>
    {
        public string? Name { get; set; }
    }
}

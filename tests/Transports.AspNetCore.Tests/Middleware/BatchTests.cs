namespace Tests.Middleware;

public class BatchTests : IDisposable
{
    private GraphQLHttpMiddlewareOptions _options = null!;
    private GraphQLHttpMiddlewareOptions _options2 = null!;
    private readonly Action<ExecutionOptions> _configureExecution = _ => { };
    private readonly TestServer _server;

    public BatchTests()
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddAutoSchema<Chat.Query>(s => s
                    .WithMutation<Chat.Mutation>()
                    .WithSubscription<Chat.Subscription>())
                .AddSchema<Schema2>()
                .AddSystemTextJson()
                .ConfigureExecutionOptions(o => _configureExecution(o)));
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app =>
        {
            app.UseWebSockets();
            app.UseGraphQL("/graphql", opts =>
            {
                _options = opts;
            });
            app.UseGraphQL<Schema2>("/graphql2", opts =>
            {
                _options2 = opts;
            });
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

    public void Dispose() => _server.Dispose();

    private Task<HttpResponseMessage> PostJsonAsync(string url, string json)
    {
        var client = _server.CreateClient();
        var content = new StringContent(json);
        content.Headers.ContentType = new("application/json");
        return client.PostAsync(url, content);
    }

    private Task<HttpResponseMessage> PostJsonAsync(string json)
        => PostJsonAsync("/graphql", json);

    private Task<HttpResponseMessage> PostBatchRequestAsync(params GraphQLRequest[] request)
        => PostJsonAsync(new GraphQLSerializer().Serialize(request));

    private Task<HttpResponseMessage> PostBatchRequestAsync(string url, params GraphQLRequest[] request)
        => PostJsonAsync(url, new GraphQLSerializer().Serialize(request));

    [Fact]
    public async Task NotParallelTest()
    {
        _options.ExecuteBatchedRequestsInParallel = false;
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "{count}" }, new GraphQLRequest() { Query = "{count}" });
        await response.ShouldBeAsync(@"[{""data"":{""count"":0}},{""data"":{""count"":0}}]");
    }

    [Fact]
    public async Task BasicTest()
    {
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "{count}" });
        await response.ShouldBeAsync(@"[{""data"":{""count"":0}}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "{invalid}" });
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""Cannot query field \u0027invalid\u0027 on type \u0027Query\u0027."",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""FIELDS_ON_CORRECT_TYPE"",""codes"":[""FIELDS_ON_CORRECT_TYPE""],""number"":""5.3.1""}}]}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disabled_BatchedRequests(bool badRequest)
    {
        _options.EnableBatchedRequests = false;
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "{count}" });
        // always returns BadRequest here
        await response.ShouldBeAsync(true, @"{""errors"":[{""message"":""Batched requests are not supported."",""extensions"":{""code"":""BATCHED_REQUESTS_NOT_SUPPORTED"",""codes"":[""BATCHED_REQUESTS_NOT_SUPPORTED""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "{" });
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""Error parsing query: Expected Name, found EOF; for more information see http://spec.graphql.org/October2021/#Field"",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""SYNTAX_ERROR"",""codes"":[""SYNTAX_ERROR""]}}]}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoQuery(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("[{}]");
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoQuery_Multiple(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("[{},{}]");
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]},{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NullRequest(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("[null]");
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NullRequest_Multiple(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("[null,null]");
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]},{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}]");
    }

    [Fact]
    public async Task Mutation()
    {
        using var response = await PostBatchRequestAsync(new GraphQLRequest { Query = "mutation{clearMessages}" });
        await response.ShouldBeAsync(@"[{""data"":{""clearMessages"":0}}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscription(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "subscription{newMessages{id}}" });
        // validation errors do not return 400 within a batch request
        await response.ShouldBeAsync(false, @"[{""errors"":[{""message"":""Subscription operations are not supported for POST requests."",""locations"":[{""line"":1,""column"":1}],""extensions"":{""code"":""HTTP_METHOD_VALIDATION"",""codes"":[""HTTP_METHOD_VALIDATION""]}}]}]");
    }

    [Fact]
    public async Task WithVariables()
    {
        using var response = await PostBatchRequestAsync("/graphql2", new GraphQLRequest()
        {
            Query = "query($test:String){var(test:$test)}",
            Variables = new Inputs(new Dictionary<string, object?> {
                { "test", "abc" }
            }),
        });
        await response.ShouldBeAsync(@"[{""data"":{""var"":""abc""}}]");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("/graphql2", @"[");
        // always returns BadRequest here
        await response.ShouldBeAsync(true, @"{""errors"":[{""message"":""JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}");
    }

    [Theory]
    [InlineData("test1", @"[{""data"":{""count"":0}}]")]
    [InlineData("test2", @"[{""data"":{""allMessages"":[]}}]")]
    public async Task OperationName(string opName, string expected)
    {
        using var response = await PostBatchRequestAsync(new GraphQLRequest() { Query = "query test1{count} query test2{allMessages{id}}", OperationName = opName });
        await response.ShouldBeAsync(expected);
    }

    [Fact]
    public async Task Extensions()
    {
        using var response = await PostJsonAsync("/graphql2", @"[{""query"":""{ext}"",""extensions"":{""test"":""abc""}}]");
        await response.ShouldBeAsync(@"[{""data"":{""ext"":""abc""}}]");
    }

    [Fact]
    public async Task DoesNotReadFromQueryString()
    {
        _options2.ReadQueryStringOnPost = true;
        var url = "/graphql2?operationName=op2";
        var request = new GraphQLRequest
        {
            Query = "query op1($test:String!){ext var(test:$test)} query op2($test:String!){var(test:$test) ext}",
            Variables = new Inputs(new Dictionary<string, object?> {
                { "test", "postvar" }
            }),
            Extensions = new Inputs(new Dictionary<string, object?> {
                { "test", "postext" }
            }),
            OperationName = "op1",
        };
        using var response = await PostBatchRequestAsync(url, request);
        await response.ShouldBeAsync(@"[{""data"":{""ext"":""postext"",""var"":""postvar""}}]");
    }

}

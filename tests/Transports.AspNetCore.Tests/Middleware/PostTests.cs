using System.Net;

namespace Tests.Middleware;

public class PostTests : IDisposable
{
    private GraphQLHttpMiddlewareOptions _options = null!;
    private GraphQLHttpMiddlewareOptions _options2 = null!;
    private readonly Action<ExecutionOptions> _configureExecution = _ => { };
    private readonly TestServer _server;

    public PostTests()
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

    private Task<HttpResponseMessage> PostRequestAsync(GraphQLRequest request)
        => PostJsonAsync(new GraphQLSerializer().Serialize(request));

    private Task<HttpResponseMessage> PostRequestAsync(string url, GraphQLRequest request)
        => PostJsonAsync(url, new GraphQLSerializer().Serialize(request));

    [Fact]
    public async Task BasicTest()
    {
        using var response = await PostRequestAsync(new() { Query = "{count}" });
        await response.ShouldBeAsync(@"{""data"":{""count"":0}}");
    }

#if NET5_0_OR_GREATER
    [Fact]
    public async Task AltCharset()
    {
        var client = _server.CreateClient();
        var content = new StringContent(@"{""query"":""{var(test:\""ë\"")}""}", Encoding.Latin1, "application/json");
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(false, @"{""data"":{""var"":""\u00EB""}}");
    }

    [Fact]
    public async Task AltCharset_Quoted()
    {
        var client = _server.CreateClient();
        var bytes = Encoding.UTF8.GetBytes(@"{""query"":""{var(test:\""ë\"")}""}");
        var content = new ByteArrayContent(bytes, 0, bytes.Length);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "\"UTF-8\"" };
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(false, @"{""data"":{""var"":""\u00EB""}}");
    }

    [Fact]
    public async Task AltCharset_Invalid()
    {
        var client = _server.CreateClient();
        var bytes = Encoding.UTF8.GetBytes(@"{""query"":""{var(test:\""ë\"")}""}");
        var content = new ByteArrayContent(bytes, 0, bytes.Length);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "unknown" };
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(HttpStatusCode.UnsupportedMediaType, @"{""errors"":[{""message"":""Invalid \u0027Content-Type\u0027 header: value \u0027application/json; charset=unknown\u0027 could not be parsed."",""extensions"":{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}]}");
    }
#endif

    [Fact]
    public async Task FormMultipart()
    {
        var client = _server.CreateClient();
        var content = new MultipartFormDataContent();
        var queryContent = new StringContent(@"query op1{ext} query op2($test:String!){ext var(test:$test)}");
        queryContent.Headers.ContentType = new("application/graphql");
        var variablesContent = new StringContent(@"{""test"":""1""}");
        variablesContent.Headers.ContentType = new("application/json");
        var extensionsContent = new StringContent(@"{""test"":""2""}");
        extensionsContent.Headers.ContentType = new("application/json");
        var operationNameContent = new StringContent("op2");
        operationNameContent.Headers.ContentType = new("text/text");
        content.Add(queryContent, "query");
        content.Add(variablesContent, "variables");
        content.Add(extensionsContent, "extensions");
        content.Add(operationNameContent, "operationName");
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(@"{""data"":{""ext"":""2"",""var"":""1""}}");
    }

    [Fact]
    public async Task FormUrlEncoded()
    {
        var client = _server.CreateClient();
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string?, string?>("query", @"query op1{ext} query op2($test:String!){ext var(test:$test)}"),
            new KeyValuePair<string?, string?>("variables", @"{""test"":""1""}"),
            new KeyValuePair<string?, string?>("extensions", @"{""test"":""2""}"),
            new KeyValuePair<string?, string?>("operationName", @"op2"),
        });
        using var response = await client.PostAsync("/graphql2", content);
        await response.ShouldBeAsync(@"{""data"":{""ext"":""2"",""var"":""1""}}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormUrlEncoded_DeserializationError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string?, string?>("query", @"{ext}"),
            new KeyValuePair<string?, string?>("variables", @"{"),
        });
        using var response = await client.PostAsync("/graphql2", content);
        // always returns BadRequest here
        await response.ShouldBeAsync(true, @"{""errors"":[{""message"":""JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}");
    }

    [Theory]
    [InlineData("application/graphql")]
    [InlineData("APPLICATION/GRAPHQL")]
    public async Task ContentType_GraphQL(string contentType)
    {
        var client = _server.CreateClient();
        var content = new StringContent("{count}");
        content.Headers.ContentType = new(contentType);
        using var response = await client.PostAsync("/graphql", content);
        await response.ShouldBeAsync(@"{""data"":{""count"":0}}");
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/graphql+json")]
    [InlineData("APPLICATION/JSON")]
    [InlineData("APPLICATION/GRAPHQL+JSON")]
    public async Task ContentType_GraphQLJson(string contentType)
    {
        var client = _server.CreateClient();
        var content = new StringContent(@"{""query"":""{count}""}");
        content.Headers.ContentType = new(contentType);
        using var response = await client.PostAsync("/graphql", content);
        await response.ShouldBeAsync(@"{""data"":{""count"":0}}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownContentType(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        var content = new StringContent("{count}");
        content.Headers.ContentType = new("application/pdf");
        using var response = await client.PostAsync("/graphql", content);
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        var actual = await response.Content.ReadAsStringAsync();
        actual.ShouldBe(@"{""errors"":[{""message"":""Invalid \u0027Content-Type\u0027 header: non-supported media type \u0027application/pdf\u0027. Must be \u0027application/json\u0027, \u0027application/graphql\u0027 or a form body."",""extensions"":{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CannotParseContentType(bool badRequest)
    {
        _options2.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        var content = new StringContent("");
        content.Headers.ContentType = null;
        var response = await client.PostAsync("/graphql2", content);
        // always returns unsupported media type
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe(@"{""errors"":[{""message"":""Invalid \u0027Content-Type\u0027 header: value \u0027\u0027 could not be parsed."",""extensions"":{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostRequestAsync(new() { Query = "{invalid}" });
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""Cannot query field \u0027invalid\u0027 on type \u0027Query\u0027."",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""FIELDS_ON_CORRECT_TYPE"",""codes"":[""FIELDS_ON_CORRECT_TYPE""],""number"":""5.3.1""}}]}");
    }

    [Fact]
    public async Task Disabled()
    {
        _options.HandlePost = false;
        using var response = await PostRequestAsync(new() { Query = "{count}" });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostRequestAsync(new() { Query = "{" });
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""Error parsing query: Expected Name, found EOF; for more information see http://spec.graphql.org/October2021/#Field"",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""SYNTAX_ERROR"",""codes"":[""SYNTAX_ERROR""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoQuery(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("{}");
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NullRequest(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("null");
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}");
    }

    [Fact]
    public async Task Mutation()
    {
        using var response = await PostRequestAsync(new() { Query = "mutation{clearMessages}" });
        await response.ShouldBeAsync(@"{""data"":{""clearMessages"":0}}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscription(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostRequestAsync(new() { Query = "subscription{newMessages{id}}" });
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""Subscription operations are not supported for POST requests."",""locations"":[{""line"":1,""column"":1}],""extensions"":{""code"":""HTTP_METHOD_VALIDATION"",""codes"":[""HTTP_METHOD_VALIDATION""]}}]}");
    }

    [Fact]
    public async Task WithVariables()
    {
        using var response = await PostRequestAsync("/graphql2", new()
        {
            Query = "query($test:String){var(test:$test)}",
            Variables = new Inputs(new Dictionary<string, object?> {
                { "test", "abc" }
            }),
        });
        await response.ShouldBeAsync(@"{""data"":{""var"":""abc""}}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        using var response = await PostJsonAsync("/graphql2", @"{");
        // always returns BadRequest here
        await response.ShouldBeAsync(true, @"{""errors"":[{""message"":""JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}");
    }

    [Theory]
    [InlineData("test1", @"{""data"":{""count"":0}}")]
    [InlineData("test2", @"{""data"":{""allMessages"":[]}}")]
    public async Task OperationName(string opName, string expected)
    {
        using var response = await PostRequestAsync(new() { Query = "query test1{count} query test2{allMessages{id}}", OperationName = opName });
        await response.ShouldBeAsync(expected);
    }

    [Fact]
    public async Task Extensions()
    {
        using var response = await PostJsonAsync("/graphql2", @"{""query"":""{ext}"",""extensions"":{""test"":""abc""}}");
        await response.ShouldBeAsync(@"{""data"":{""ext"":""abc""}}");
    }

    [Theory]
    [InlineData(false, false, false, @"{""data"":{""ext"":""postext"",""var"":""postvar""}}")]
    [InlineData(true, false, false, @"{""data"":{""var"":""postvar"",""altext"":""postext""}}")]
    [InlineData(true, true, false, @"{""data"":{""var"":""urlvar"",""altext"":""postext""}}")]
    [InlineData(true, false, true, @"{""data"":{""var"":""postvar"",""altext"":""urlext""}}")]
    [InlineData(true, true, true, @"{""data"":{""var"":""urlvar"",""altext"":""urlext""}}")]
    public async Task ReadAlsoFromQueryString(bool readFromQueryString, bool readVariablesFromQueryString, bool readExtensionsFromQueryString, string expected)
    {
        _options2.ReadQueryStringOnPost = readFromQueryString;
        _options2.ReadVariablesFromQueryString = readVariablesFromQueryString;
        _options2.ReadExtensionsFromQueryString = readExtensionsFromQueryString;
        var url = "/graphql2?query=query op1($test:String!){altext:ext var(test:$test)} query op2($test:String!){var(test:$test) altext:ext}&operationName=op2&variables={%22test%22:%22urlvar%22}&extensions={%22test%22:%22urlext%22}";
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
        using var response = await PostRequestAsync(url, request);
        await response.ShouldBeAsync(expected);
    }

}

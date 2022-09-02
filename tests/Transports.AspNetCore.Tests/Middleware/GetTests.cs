using System.Net;

namespace Tests.Middleware;

public class GetTests : IDisposable
{
    private GraphQLHttpMiddlewareOptions _options = null!;
    private GraphQLHttpMiddlewareOptions _options2 = null!;
    private readonly Action<ExecutionOptions> _configureExecution = _ => { };
    private readonly TestServer _server;

    public GetTests()
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
        public static string? Ext(IResolveFieldContext context)
            => context.InputExtensions.TryGetValue("test", out var value) ? value?.ToString() : null;
    }

    public void Dispose() => _server.Dispose();

    [Fact]
    public async Task BasicTest()
    {
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query={count}");
        await response.ShouldBeAsync(@"{""data"":{""count"":0}}");
    }

    [Theory]
    [InlineData(null, "application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData(null, "application/json", "application/json; charset=utf-8")]
    [InlineData(null, "application/json; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData(null, "application/json; charset=UTF-8", "application/json; charset=utf-8")]
    [InlineData(null, "APPLICATION/JSON", "application/json; charset=utf-8")]
    [InlineData(null, "APPLICATION/JSON; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData(null, "*/*; CHARSET=\"UTF-8\" ", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/*; charset=utf-8", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/*+json; charset=utf-8", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/pdf", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/json; charset=utf-7", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/json", "application/json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/json; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/json; charset=UTF-8", "application/json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "APPLICATION/JSON", "application/json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "APPLICATION/JSON; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "*/*; CHARSET=\"UTF-8\" ", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/*; charset=utf-8", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/*+json; charset=utf-8", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/pdf", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/graphql-response+json; charset=utf-8", "application/json; charset=utf-7", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/json", "application/json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/json; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/json; charset=UTF-8", "application/json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "APPLICATION/JSON", "application/json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "APPLICATION/JSON; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "*/*; CHARSET=\"UTF-8\" ", "application/graphql+json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/*; charset=utf-8", "application/graphql+json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/*+json; charset=utf-8", "application/graphql+json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/pdf", "application/graphql+json; charset=utf-8")]
    [InlineData("application/graphql+json; charset=utf-8", "application/json; charset=utf-7", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/json", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/json; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/json; charset=UTF-8", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "APPLICATION/JSON", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "APPLICATION/JSON; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "*/*; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/*; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/*+json; charset=utf-8", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/pdf", "application/json; charset=utf-8")]
    [InlineData("application/json; charset=utf-8", "application/json; charset=utf-7", "application/json; charset=utf-8")]
    [InlineData("application/json", "application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json", "application/json", "application/json")]
    [InlineData("application/json", "application/json; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData("application/json", "application/json; charset=UTF-8", "application/json; charset=utf-8")]
    [InlineData("application/json", "APPLICATION/JSON", "application/json")]
    [InlineData("application/json", "APPLICATION/JSON; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData("application/json", "*/*; CHARSET=\"UTF-8\" ", "application/json; charset=utf-8")]
    [InlineData("application/json", "application/*; charset=utf-8", "application/json; charset=utf-8")]
    [InlineData("application/json", "application/*+json; charset=utf-8", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/json", "application/pdf", "application/json")]
    [InlineData("application/json", "application/json; charset=utf-7", "application/json")]
    [InlineData(null, "*/*, application/graphql-response+json", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "*/*, application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData(null, "*/*, application/json", "application/json; charset=utf-8")]
    [InlineData(null, "*/*, application/pdf", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/*, application/graphql-response+json", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/*, application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData(null, "application/*, application/json", "application/json; charset=utf-8")]
    [InlineData(null, "application/*, application/pdf", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/*+json, application/graphql-response+json", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/*+json, application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData(null, "application/*+json, application/json", "application/json; charset=utf-8")]
    [InlineData(null, "application/*+json, application/pdf", "application/graphql-response+json; charset=utf-8")]
    [InlineData(null, "application/graphql+json, application/json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json", "*/*, application/graphql-response+json", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/json", "*/*, application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json", "*/*, application/json", "application/json")]
    [InlineData("application/json", "*/*, application/pdf", "application/json")]
    [InlineData("application/json", "application/*, application/graphql-response+json", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/json", "application/*, application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json", "application/*, application/json", "application/json")]
    [InlineData("application/json", "application/*, application/pdf", "application/json")]
    [InlineData("application/json", "application/*+json, application/graphql-response+json", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/json", "application/*+json, application/graphql+json", "application/graphql+json; charset=utf-8")]
    [InlineData("application/json", "application/*+json, application/json", "application/json")]
    [InlineData("application/json", "application/*+json, application/pdf", "application/graphql-response+json; charset=utf-8")]
    [InlineData("application/json", "application/graphql+json, application/json", "application/graphql+json; charset=utf-8")]
    public async Task AcceptHeaderHonored(string? defaultMediaType, string mediaType, string expected)
    {
        if (defaultMediaType != null)
        {
            _options.DefaultResponseContentType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(defaultMediaType);
        }
        var client = _server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/graphql?query={count}");
        request.Headers.Add("Accept", mediaType);
        using var response = await client.SendAsync(request);
        var contentType = response.Content.Headers.ContentType?.ToString();
        contentType.ShouldBe(expected);
        (await response.Content.ReadAsStringAsync()).ShouldBe(@"{""data"":{""count"":0}}");
    }

    [Fact]
    public async Task NoUseWebSockets()
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddAutoSchema<Chat.Query>()
                .AddSystemTextJson());
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app => app.UseGraphQL());
        using var server = new TestServer(hostBuilder);

        var client = server.CreateClient();
        using var response = await client.GetAsync("/graphql?query={count}");
        await response.ShouldBeAsync(@"{""data"":{""count"":0}}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query={invalid}");
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""Cannot query field \u0027invalid\u0027 on type \u0027Query\u0027."",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""FIELDS_ON_CORRECT_TYPE"",""codes"":[""FIELDS_ON_CORRECT_TYPE""],""number"":""5.3.1""}}]}");
    }

    [Fact]
    public async Task Disabled()
    {
        _options.HandleGet = false;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query={count}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query={");
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""Error parsing query: Expected Name, found EOF; for more information see http://spec.graphql.org/October2021/#Field"",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""SYNTAX_ERROR"",""codes"":[""SYNTAX_ERROR""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NoQuery(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql");
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyQuery(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query=");
        await response.ShouldBeAsync(badRequest, @"{""errors"":[{""message"":""Document does not contain any operations."",""extensions"":{""code"":""NO_OPERATION"",""codes"":[""NO_OPERATION""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mutation(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query=mutation{clearMessages}");
        await response.ShouldBeAsync(HttpStatusCode.MethodNotAllowed, @"{""errors"":[{""message"":""Only query operations allowed for GET requests."",""locations"":[{""line"":1,""column"":1}],""extensions"":{""code"":""HTTP_METHOD_VALIDATION"",""codes"":[""HTTP_METHOD_VALIDATION""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Subscription(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query=subscription{newMessages{id}}");
        await response.ShouldBeAsync(HttpStatusCode.MethodNotAllowed, @"{""errors"":[{""message"":""Only query operations allowed for GET requests."",""locations"":[{""line"":1,""column"":1}],""extensions"":{""code"":""HTTP_METHOD_VALIDATION"",""codes"":[""HTTP_METHOD_VALIDATION""]}}]}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithVariables(bool readVariablesFromQueryString)
    {
        _options.ReadVariablesFromQueryString = readVariablesFromQueryString;
        _options.ValidationErrorsReturnBadRequest = false;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query=query($from:String!){allMessages(from:$from){id}}&variables={%22from%22:%22abc%22}");
        if (readVariablesFromQueryString)
        {
            await response.ShouldBeAsync(@"{""data"":{""allMessages"":[]}}");
        }
        else
        {
            await response.ShouldBeAsync(@"{""errors"":[{""message"":""Variable \u0027$from\u0027 is invalid. No value provided for a non-null variable."",""locations"":[{""line"":1,""column"":7}],""extensions"":{""code"":""INVALID_VALUE"",""codes"":[""INVALID_VALUE""],""number"":""5.8""}}]}");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VariableParseError(bool badRequest)
    {
        _options.ValidationErrorsReturnBadRequest = badRequest;
        _options.ReadVariablesFromQueryString = true;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query=query($from:String!){allMessages(from:$from){id}}&variables={");
        // always returns BadRequest here
        await response.ShouldBeAsync(true, @"{""errors"":[{""message"":""JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}");
    }

    [Theory]
    [InlineData("test1", @"{""data"":{""count"":0}}")]
    [InlineData("test2", @"{""data"":{""allMessages"":[]}}")]
    public async Task OperationName(string opName, string expected)
    {
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql?query=query test1{count} query test2{allMessages{id}}&operationName=" + opName);
        await response.ShouldBeAsync(expected);
    }

    [Theory]
    [InlineData(true, @"{""data"":{""ext"":""abc""}}")]
    [InlineData(false, @"{""data"":{""ext"":null}}")]
    public async Task Extensions(bool readExtensions, string expected)
    {
        _options2.ReadExtensionsFromQueryString = readExtensions;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql2?query={ext}&extensions={%22test%22:%22abc%22}");
        await response.ShouldBeAsync(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExtensionsParseError(bool badRequest)
    {
        _options2.ReadExtensionsFromQueryString = true;
        _options2.ValidationErrorsReturnBadRequest = badRequest;
        var client = _server.CreateClient();
        using var response = await client.GetAsync("/graphql2?query={ext}&extensions={");
        // always returns BadRequest here
        await response.ShouldBeAsync(true, @"{""errors"":[{""message"":""JSON body text could not be parsed. Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. Path: $ | LineNumber: 0 | BytePositionInLine: 1."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}");
    }
}

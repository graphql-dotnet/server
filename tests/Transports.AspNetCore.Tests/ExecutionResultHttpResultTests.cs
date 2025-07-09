#if NET6_0_OR_GREATER
using System.Globalization;

namespace Tests;

public class ExecutionResultHttpResultTests : IDisposable
{
    private readonly TestServer _server;

    public ExecutionResultHttpResultTests()
    {
        var _hostBuilder = new WebHostBuilder();
        _hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddAutoSchema<Chat.Query>()
                .AddSystemTextJson());
            services.AddRouting();
        });
        _hostBuilder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/graphql", async (HttpContext context, IDocumentExecuter<ISchema> documentExecuter) =>
                {
                    var query = context.Request.Query["query"].ToString();
                    var resultCodeStr = context.Request.Query["resultCode"].ToString();
                    var jsonType = context.Request.Query["jsonType"].ToString() == "true";

                    var result = await documentExecuter.ExecuteAsync(new()
                    {
                        Query = query,
                        RequestServices = context.RequestServices,
                        CancellationToken = context.RequestAborted,
                    });

                    ExecutionResultHttpResult result2;
                    if (string.IsNullOrEmpty(resultCodeStr))
                        result2 = new ExecutionResultHttpResult(result);
                    else
                        result2 = new ExecutionResultHttpResult(result, (System.Net.HttpStatusCode)int.Parse(resultCodeStr, CultureInfo.InvariantCulture));

                    if (jsonType)
                        result2.ContentType = "application/json";

                    return result2;
                });
            });
        });
        _server = new TestServer(_hostBuilder);
    }

    [Fact]
    public async Task Basic()
    {
        var str = await _server.ExecuteGet("/graphql?query={count}");
        str.ShouldBe("{\"data\":{\"count\":0}}");
    }

    [Fact]
    public async Task ForcedResultCode()
    {
        var str2 = await _server.ExecuteGet("/graphql?query={}&resultCode=200");
        str2.ShouldBe("""{"errors":[{"message":"Error parsing query: Expected Name, found }; for more information see http://spec.graphql.org/October2021/#Field","locations":[{"line":1,"column":2}],"extensions":{"code":"SYNTAX_ERROR","codes":["SYNTAX_ERROR"]}}]}""");
    }

    [Fact]
    public async Task AltContentType()
    {
        using var httpClient = _server.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/graphql?query={count}&jsonType=true");
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var contentType = response.Content.Headers.ContentType;
        contentType.ShouldNotBeNull();
        contentType.MediaType.ShouldBe("application/json");
        var str3 = await response.Content.ReadAsStringAsync();
        str3.ShouldBe("{\"data\":{\"count\":0}}");
    }

    [Fact]
    public async Task ReturnsBadRequestForUnexecutedResults()
    {
        using var httpClient = _server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/graphql?query={}");
        var response = await httpClient.SendAsync(request);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        var str3 = await response.Content.ReadAsStringAsync();
        str3.ShouldBe("""{"errors":[{"message":"Error parsing query: Expected Name, found }; for more information see http://spec.graphql.org/October2021/#Field","locations":[{"line":1,"column":2}],"extensions":{"code":"SYNTAX_ERROR","codes":["SYNTAX_ERROR"]}}]}""");
    }

    public void Dispose() => _server.Dispose();
}
#endif
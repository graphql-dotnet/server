using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class ExecutionResultActionResultTests : IDisposable
{
    private readonly TestServer _server;

    public ExecutionResultActionResultTests()
    {
        var _hostBuilder = new WebHostBuilder();
        _hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton<Chat.IChat, Chat.Chat>();
            services.AddGraphQL(b => b
                .AddAutoSchema<Chat.Query>()
                .AddSystemTextJson());
            services.AddRouting();
#if NETCOREAPP2_1 || NET48
            services.AddMvc();
#else
            services.AddControllers();
#endif
        });
        _hostBuilder.Configure(app =>
        {
#if NETCOREAPP2_1 || NET48
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
#else
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
#endif
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

[Route("/")]
public class TestController : Controller
{
    private readonly IDocumentExecuter _documentExecuter;

    public TestController(IDocumentExecuter<ISchema> documentExecuter)
    {
        _documentExecuter = documentExecuter;
    }

    [HttpGet]
    [Route("graphql")]
    public async Task<IActionResult> Test(string query, int? resultCode = null, bool jsonType = false)
    {
        var result = await _documentExecuter.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = HttpContext.RequestServices,
            CancellationToken = HttpContext.RequestAborted,
        });

        ExecutionResultActionResult result2;
        if (resultCode == null)
            result2 = new ExecutionResultActionResult(result);
        else
            result2 = new ExecutionResultActionResult(result, (System.Net.HttpStatusCode)resultCode.Value);

        if (jsonType)
            result2.ContentType = "application/json";

        return result2;
    }
}

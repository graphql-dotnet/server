using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class ExecutionResultActionResultTests
{
    [Fact]
    public async Task Basic()
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
        var server = new TestServer(_hostBuilder);

        var str = await server.ExecuteGet("/graphql?query={count}");
        str.ShouldBe("{\"data\":{\"count\":0}}");

        var str2 = await server.ExecuteGet("/graphql?query={}&resultCode=200");
        str2.ShouldBe(@"{""errors"":[{""message"":""Error parsing query: Expected Name, found }; for more information see http://spec.graphql.org/October2021/#Field"",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""SYNTAX_ERROR"",""codes"":[""SYNTAX_ERROR""]}}]}");

        using var httpClient = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/graphql?query={}");
        var response = await httpClient.SendAsync(request);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        var str3 = await response.Content.ReadAsStringAsync();
        str3.ShouldBe(@"{""errors"":[{""message"":""Error parsing query: Expected Name, found }; for more information see http://spec.graphql.org/October2021/#Field"",""locations"":[{""line"":1,""column"":2}],""extensions"":{""code"":""SYNTAX_ERROR"",""codes"":[""SYNTAX_ERROR""]}}]}");
    }
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
    public async Task<IActionResult> Test(string query, int? resultCode = null)
    {
        var result = await _documentExecuter.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = HttpContext.RequestServices,
            CancellationToken = HttpContext.RequestAborted,
        });
        if (resultCode == null)
            return new ExecutionResultActionResult(result);
        else
            return new ExecutionResultActionResult(result, (System.Net.HttpStatusCode)resultCode.Value);
    }
}

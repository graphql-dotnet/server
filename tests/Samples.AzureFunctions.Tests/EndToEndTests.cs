using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Samples.AzureFunctions.Tests;

public class EndToEndTests
{
    [Fact]
    public async Task GraphQLGet()
    {
        var (statusCode, contentType, body) = await ExecuteRequest(request =>
        {
            request.Method = "GET";
            request.QueryString = new QueryString("?query={count}");
        }, GraphQL.RunGraphQL).ConfigureAwait(false);

        statusCode.ShouldBe(200);
        contentType.ShouldBe("application/graphql-response+json; charset=utf-8");
        body.ShouldBe("""{"data":{"count":0}}""");
    }

    [Fact]
    public async Task GraphQLPost()
    {
        var (statusCode, contentType, body) = await ExecuteRequest(request =>
        {
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"query":"{count}"}"""));
        }, GraphQL.RunGraphQL).ConfigureAwait(false);

        statusCode.ShouldBe(200);
        contentType.ShouldBe("application/graphql-response+json; charset=utf-8");
        body.ShouldBe("""{"data":{"count":0}}""");
    }

    [Fact]
    public async Task Playground()
    {
        var (statusCode, contentType, body) = await ExecuteRequest(request =>
        {
            request.Method = "GET";
        }, GraphQL.RunPlayground).ConfigureAwait(false);

        statusCode.ShouldBe(200);
        contentType.ShouldBe("text/html");
        body.ShouldContain("<!DOCTYPE html>", Case.Insensitive);
        body.ShouldContain("playground", Case.Insensitive);
    }

    private async Task<(int statusCode, string? contentType, string body)> ExecuteRequest(Action<HttpRequest> configureRequest, Func<HttpRequest, ILogger, IActionResult> func)
    {
        // run startup
        var services = new ServiceCollection();
        var builderMock = new Mock<IFunctionsHostBuilder>(MockBehavior.Strict);
        builderMock.Setup(x => x.Services).Returns(services);
        new Startup().Configure(builderMock.Object);

        // build request
        var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext() { RequestServices = provider };
        var stream = new MemoryStream();
        context.Response.Body = stream;
        var request = context.Request;
        configureRequest(request);

        // execute request
        var result = func(request, Mock.Of<ILogger>());
        await result.ExecuteResultAsync(new ActionContext() { HttpContext = context }).ConfigureAwait(false);

        // return result
        return (context.Response.StatusCode, context.Response.ContentType, Encoding.UTF8.GetString(stream.ToArray()));
    }
}

using System.Net;

namespace Tests;

internal static class ShouldlyExtensions
{
    public static Task ShouldBeAsync(this HttpResponseMessage message, bool badRequest, string expectedResponse)
        => ShouldBeAsync(message, badRequest ? HttpStatusCode.BadRequest : HttpStatusCode.OK, expectedResponse);

    public static Task ShouldBeAsync(this HttpResponseMessage message, string expectedResponse)
        => ShouldBeAsync(message, HttpStatusCode.OK, expectedResponse);

    public static Task ShouldBeAsync(this HttpResponseMessage message, HttpStatusCode httpStatusCode, string expectedResponse)
        => ShouldBeAsync(message, "application/graphql-response+json; charset=utf-8", httpStatusCode, expectedResponse);

    public static async Task ShouldBeAsync(this HttpResponseMessage message, string contentType, HttpStatusCode httpStatusCode, string expectedResponse)
    {
        message.StatusCode.ShouldBe(httpStatusCode);
        (message.Content.Headers.ContentType?.ToString()).ShouldBe(contentType);
        var actualResponse = await message.Content.ReadAsStringAsync();
        actualResponse.ShouldBe(expectedResponse);
    }
}

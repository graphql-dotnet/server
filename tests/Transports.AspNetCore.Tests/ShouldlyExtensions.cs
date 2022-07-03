using System.Net;

namespace Tests;

internal static class ShouldlyExtensions
{
    public static Task ShouldBeAsync(this HttpResponseMessage message, bool badRequest, string expectedResponse)
        => ShouldBeAsync(message, badRequest ? HttpStatusCode.BadRequest : HttpStatusCode.OK, expectedResponse);

    public static Task ShouldBeAsync(this HttpResponseMessage message, string expectedResponse)
        => ShouldBeAsync(message, HttpStatusCode.OK, expectedResponse);

    public static async Task ShouldBeAsync(this HttpResponseMessage message, HttpStatusCode httpStatusCode, string expectedResponse)
    {
        message.StatusCode.ShouldBe(httpStatusCode);
        message.Content.Headers.ContentType?.MediaType.ShouldBe("application/graphql+json");
        message.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
        var actualResponse = await message.Content.ReadAsStringAsync();
        actualResponse.ShouldBe(expectedResponse);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImageMagick;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Samples.Upload.Tests;

public class EndToEndTests
{
    [Theory]
    [InlineData(MagickFormat.Jpeg, "image/jpeg", "image.jpg")]
    [InlineData(MagickFormat.Png, "image/png", "image.png")]
    public async Task RotateImage(MagickFormat format, string contentType, string fileName)
    {
        using var webApp = new WebApplicationFactory<Program>();
        var server = webApp.Server;

        using var client = server.CreateClient();
        var form = new MultipartFormDataContent();
        var operations = new
        {
            query = "mutation ($img: FormFile!) { rotate(file: $img) }",
            variables = new { img = (string?)null },
        };
        form.Add(JsonContent.Create(operations), "operations");
        var map = new
        {
            file0 = new string[] { "variables.img" },
        };
        form.Add(JsonContent.Create(map), "map");
        // A non-square image with a red left half and a blue right half.
        using var image = new MagickImage(MagickColors.Red, 40, 20);
        using var blueHalf = new MagickImage(MagickColors.Blue, 20, 20);
        image.Composite(blueHalf, 20, 0, CompositeOperator.Src);
        var imageContent = new ByteArrayContent(image.ToByteArray(format));
        imageContent.Headers.ContentType = new(contentType);
        form.Add(imageContent, "file0", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = form;
        request.Headers.Add("GraphQL-Require-Preflight", "true");
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        result.RootElement.TryGetProperty("errors", out _).ShouldBeFalse();
        string base64Image = result.RootElement.GetProperty("data").GetProperty("rotate").GetString()!;
        using var rotatedImage = new MagickImage(Convert.FromBase64String(base64Image));
        rotatedImage.Format.ShouldBe(MagickFormat.Jpeg);
        rotatedImage.Width.ShouldBe(20u);
        rotatedImage.Height.ShouldBe(40u);

        // After a clockwise quarter turn, the red half is above the blue half.
        // Allow for JPEG compression when checking colors away from the boundary.
        using var pixels = rotatedImage.GetPixels();
        var top = pixels.GetPixel(10, 10).ToColor()!;
        top.R.ShouldBeGreaterThan((byte)200);
        top.G.ShouldBeLessThan((byte)40);
        top.B.ShouldBeLessThan((byte)40);
        var bottom = pixels.GetPixel(10, 30).ToColor()!;
        bottom.R.ShouldBeLessThan((byte)40);
        bottom.G.ShouldBeLessThan((byte)40);
        bottom.B.ShouldBeGreaterThan((byte)200);
    }

    [Fact]
    public async Task RotateImage_WrongType()
    {
        using var webApp = new WebApplicationFactory<Program>();
        var server = webApp.Server;

        using var client = server.CreateClient();
        var form = new MultipartFormDataContent();
        var operations = new
        {
            query = "mutation ($img: FormFile!) { rotate(file: $img) }",
            variables = new { img = (string?)null },
        };
        form.Add(JsonContent.Create(operations), "operations");
        var map = new
        {
            file0 = new string[] { "variables.img" },
        };
        form.Add(JsonContent.Create(map), "map");
        // base 64 of hello world
        var base64hello = "aGVsbG8gd29ybGQ=";
        var triangle = Convert.FromBase64String(base64hello);
        var triangleContent = new ByteArrayContent(triangle);
        triangleContent.Headers.ContentType = new("text/text");
        form.Add(triangleContent, "file0", "hello-world.txt");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = form;
        request.Headers.Add("GraphQL-Require-Preflight", "true");
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe("{\"errors\":[{\"message\":\"Invalid value for argument \\u0027file\\u0027 of field \\u0027rotate\\u0027. Invalid media type \\u0027text/text\\u0027.\",\"locations\":[{\"line\":1,\"column\":43}],\"extensions\":{\"code\":\"INVALID_VALUE\",\"codes\":[\"INVALID_VALUE\",\"INVALID_OPERATION\"],\"number\":\"5.6\"}}]}");
    }
}

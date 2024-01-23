using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Samples.Upload.Tests;

public class EndToEndTests
{
    [Fact]
    public async Task RotateImage()
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
        // jpeg image of a red triangle
        var base64triangle = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAAgACADASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD3+obq6hsrOe7uH2QQRtJI2CdqqMk4HPQVNXmPxg8Q/ZtOt9Bhb95dYmn46RqflHI7sM8HI2ehrOtUVODkzty7BSxuJhQj1evkur+79DvdC1eHXtDs9UgG1LiPcV5Oxhwy5IGcMCM45xWjXjXwf8Q/ZtRuNBmb93dZmg46SKPmHA7qM8nA2epr2Wpw9X2tNSNs3wDwOLlR6br0e33bfIhurqGys57u4fZBBG0kjYJ2qoyTgc9BXzHrurza9rl5qk42vcSbgvB2KOFXIAzhQBnHOK978daJq/iLQxpely2sSSyBrhp3Iyq8hQAp/iwc5H3e+TXm3/Cm/EP/AD+aX/39k/8AiK5MbGrUajGOiPf4ZrYHCQlWr1Upy0s+iXy6v8EcFa3U1leQXdu+yeCRZI2wDtZTkHB46ivpzQtXh17Q7PVIBtS4j3FeTsYcMuSBnDAjOOcV5B/wpvxD/wA/ml/9/ZP/AIiu7+H3hjW/CkN3Z6hPZzWczCWPyJGLJJ0PBQZBAHfjb05NTg4Vac7Si7M24kxGAxtBTo1U5x/FPdbfP7z/2Q==";
        var triangle = Convert.FromBase64String(base64triangle);
        var triangleContent = new ByteArrayContent(triangle);
        triangleContent.Headers.ContentType = new("image/jpeg");
        form.Add(triangleContent, "file0", "triangle.jpg");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = form;
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe("{\"data\":{\"rotate\":\"/9j/4AAQSkZJRgABAQAAAQABAAD/2wCEAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDIBCQkJDAsMGA0NGDIhHCEyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMv/AABEIACAAIAMBIgACEQEDEQH/xAGiAAABBQEBAQEBAQAAAAAAAAAAAQIDBAUGBwgJCgsQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5\\u002BgEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoLEQACAQIEBAMEBwUEBAABAncAAQIDEQQFITEGEkFRB2FxEyIygQgUQpGhscEJIzNS8BVictEKFiQ04SXxFxgZGiYnKCkqNTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqCg4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2dri4\\u002BTl5ufo6ery8/T19vf4\\u002Bfr/2gAMAwEAAhEDEQA/APUNa8c6boGvppeorLFG8Kyi5UblXlgQwHzdh0B69sZrorS8tr\\u002B2We0uY54X\\u002B7LGwZWwcHBHHWvGfi\\u002Bf\\u002BKwh5z/oSf8Aob1x\\u002Bla1qOiXP2jTbuS3kPXbyrcEDKng4ycZBxXnzxjp1ZRkro\\u002BsocNxxeCp16MuWbV3fVPf5r8UfUGSME8etDMBk\\u002BleX\\u002BH/AIt2twyQ65AbZ\\u002Bc3EILRnqeV\\u002B8Ow43ZPoK9HtLy2v7ZLi0njmhfO2SNtynBxwRx1rtp1YVFeLPncZgMTg5cteDXn0fo9v62PGPi//wAjhB/15J/6G9cLb2093OsFtBLNM2cRxoXZsDJwBz0r3DxL8P18UeJYr\\u002B7vGgtEt1i8uJcyMwLk8ngDlexzz0610mlaFpmgW7R6bZRwI33ivLNgkjcx5OMnGelcE8HKpVlJ6K59RhuIqODwFOjBc00vRLfd/wCX3nmGg/CS\\u002BugJdcn\\u002ByJ/zxhIeQ9Ry3QdjxuyPQ16fpXh/TNBhaLTLOO3VsZK8s2CSNzHk4ycZ6VsU0Z9K7aVCFL4UfO47NcVjn\\u002B\\u002Blp2Wi\\u002B7r87n//2Q==\"}}");
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
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var ret = await response.Content.ReadAsStringAsync();
        ret.ShouldBe("{\"errors\":[{\"message\":\"Invalid value for argument \\u0027file\\u0027 of field \\u0027rotate\\u0027. Invalid media type \\u0027text/text\\u0027.\",\"locations\":[{\"line\":1,\"column\":43}],\"extensions\":{\"code\":\"INVALID_VALUE\",\"codes\":[\"INVALID_VALUE\",\"INVALID_OPERATION\"],\"number\":\"5.6\"}}]}");
    }
}

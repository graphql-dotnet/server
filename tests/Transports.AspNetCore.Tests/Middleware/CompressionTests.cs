#if !NETCOREAPP2_1 && !NET48

using System.IO.Compression;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.ResponseCompression;

namespace Tests.Middleware;

public class CompressionTests
{
    [Fact]
    public async Task ResponseCompression_ShouldCompressGraphQLResponse()
    {
        // Arrange
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddGraphQL(b => b
                .AddAutoSchema<Query>()
                .AddSystemTextJson());
            services.AddRouting();
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Append("application/graphql-response+json");
            });
        });
        hostBuilder.Configure(app =>
        {
            app.UseResponseCompression();
            app.UseGraphQL();
        });

        using var server = new TestServer(hostBuilder);
        using var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        var content = new StringContent("{hello}");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/graphql");
        request.Content = content;
        request.Headers.Add("Accept-Encoding", "gzip");

        using var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        // Check if response is compressed
        response.Content.Headers.ContentEncoding.ShouldContain("gzip");

        // Decompress and verify content
        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var decompressionStream = new GZipStream(responseStream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompressionStream);
        var decompressedContent = await reader.ReadToEndAsync();

        decompressedContent.ShouldBe("""{"data":{"hello":"world"}}""");
    }

    public class Query
    {
        public static string Hello => "world";
    }

#if NET7_0_OR_GREATER
    [Fact]
    public async Task RequestDecompression_Supported()
    {
        // Arrange
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddGraphQL(b => b
                .AddAutoSchema<Query>()
                .AddSystemTextJson());
            services.AddRouting();
            services.AddRequestDecompression();
        });
        hostBuilder.Configure(app =>
        {
            app.UseRequestDecompression();
            app.UseGraphQL();
        });

        using var server = new TestServer(hostBuilder);
        using var client = server.CreateClient();

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        var originalContent = "{hello}";
        var compressedContentStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedContentStream, CompressionMode.Compress, true))
        {
            using var writer = new StreamWriter(gzipStream);
            writer.Write(originalContent);
        }
        compressedContentStream.Seek(0, SeekOrigin.Begin);
        request.Content = new StreamContent(compressedContentStream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/graphql");
        request.Content.Headers.ContentEncoding.Add("gzip");

        using var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        // verify content
        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.ShouldBe("""{"data":{"hello":"world"}}""");
    }
#endif
}

#endif

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Tests.Middleware.Cors;

public class MiddlewareTests
{
    public async Task<CorsResponse> ExecuteMiddleware(
        HttpMethod method,
        Action<CorsPolicyBuilder>? configureCorsPolicy,
        Action<GraphQLHttpMiddlewareOptions> configureGraphQl,
        Action<HttpRequestHeaders> configureHeaders)
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddGraphQL(b => b
                .AddAutoSchema<Query>()
                .AddSystemTextJson());
            services.AddCors();
#if NETCOREAPP2_1 || NET48
            services.AddHostApplicationLifetime();
#endif
        });
        hostBuilder.Configure(app =>
        {
            if (configureCorsPolicy != null)
                app.UseCors(configureCorsPolicy);
            else
                app.UseCors();
            app.UseGraphQL(configureMiddleware: configureGraphQl);
        });
        using var server = new TestServer(hostBuilder);
        using var client = server.CreateClient();
        var request = new HttpRequestMessage(method, "/graphql");
        if (method == HttpMethod.Post)
        {
            var content = new StringContent("{hello}");
            content.Headers.ContentType = new("application/graphql");
            request.Content = content;
        }
        else
        {
            request.Headers.Add("Access-Control-Request-Method", "POST");
        }
        configureHeaders(request.Headers);
        using var response = await client.SendAsync(request);
        if (method == HttpMethod.Post)
        {
            (await response.Content.ReadAsStringAsync()).ShouldBe(@"{""data"":{""hello"":""world""}}");
        }
        response.EnsureSuccessStatusCode();
        return new CorsResponse
        {
            AllowCredentials = response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var values) ? bool.Parse(values.Single()) : null,
            AllowHeaders = response.Headers.TryGetValues("Access-Control-Allow-Headers", out var values2) ? values2.Single() : null,
            AllowMethods = response.Headers.TryGetValues("Access-Control-Allow-Methods", out var values3) ? values3.Single() : null,
            AllowOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values4) ? values4.Single() : null,
        };
    }

    public class Query
    {
        public static string Hello => "world";
    }

    public class CorsResponse
    {
        public bool? AllowCredentials { get; set; }
        public string? AllowHeaders { get; set; }
        public string? AllowMethods { get; set; }
        public string? AllowOrigin { get; set; }
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("OPTIONS")]
    public async Task NoPolicy(string httpMethod)
    {
        var ret = await ExecuteMiddleware(
            httpMethod == "POST" ? HttpMethod.Post : httpMethod == "OPTIONS" ? HttpMethod.Options : throw new ArgumentOutOfRangeException(nameof(httpMethod)),
            configureCorsPolicy: _ => { },
            configureGraphQl: _ => { },
            configureHeaders: headers =>
            {
                headers.Add("Origin", "http://www.example.com");
            });

        ret.AllowCredentials.ShouldBeNull();
        ret.AllowHeaders.ShouldBeNull();
        ret.AllowMethods.ShouldBeNull();
        ret.AllowOrigin.ShouldBeNull();
    }

    [Theory]
    [InlineData("POST", true)]
    [InlineData("POST", false)]
    [InlineData("OPTIONS", true)]
    [InlineData("OPTIONS", false)]
    public async Task DefaultOriginPolicy(string httpMethod, bool pass)
    {
        var ret = await ExecuteMiddleware(
            httpMethod == "POST" ? HttpMethod.Post : httpMethod == "OPTIONS" ? HttpMethod.Options : throw new ArgumentOutOfRangeException(nameof(httpMethod)),
            configureCorsPolicy: b =>
            {
                b.AllowCredentials();
                b.WithOrigins("http://www.example.com", "http://www.example2.com");
                b.WithMethods("POST");
            },
            configureGraphQl: _ => { },
            configureHeaders: headers =>
            {
                headers.Add("Origin", pass ? "http://www.example.com" : "http://www.dummy.com");
            });

        ret.AllowHeaders.ShouldBeNull();
        if (pass)
        {
            ret.AllowCredentials.ShouldBe(true);
            ret.AllowOrigin.ShouldBe("http://www.example.com");
#if !NET48 && !NETCOREAPP2_1
            if (httpMethod == "OPTIONS")
            {
                ret.AllowMethods.ShouldBe("POST");
            }
#endif
        }
        else
        {
            ret.AllowCredentials.ShouldBeNull();
            ret.AllowOrigin.ShouldBeNull();
            ret.AllowMethods.ShouldBeNull();
        }
    }
}

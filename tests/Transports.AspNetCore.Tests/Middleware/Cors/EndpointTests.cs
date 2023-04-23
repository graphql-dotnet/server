#if !NETCOREAPP2_1 && !NET48

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Tests.Middleware.Cors;

public class EndpointTests
{
    private async Task<CorsResponse> ExecuteEndpoint(
        HttpMethod method,
        Action<CorsOptions> configureCors,
        Action<CorsPolicyBuilder>? configureCorsPolicy,
        Action<GraphQLHttpMiddlewareOptions> configureGraphQl,
        Action<GraphQLEndpointConventionBuilder> configureGraphQlEndpoint,
        Action<HttpRequestHeaders> configureHeaders,
        string url)
    {
        var hostBuilder = new WebHostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddGraphQL(b => b
                .AddAutoSchema<Query>()
                .AddSystemTextJson());
            services.AddCors(configureCors);
            services.AddRouting();
        });
        hostBuilder.Configure(app =>
        {
            app.UseRouting();
            if (configureCorsPolicy != null)
                app.UseCors(configureCorsPolicy);
            else
                app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                var ep = endpoints.MapGraphQL(configureMiddleware: configureGraphQl);
                configureGraphQlEndpoint(ep);
            });
        });
        using var server = new TestServer(hostBuilder);
        using var client = server.CreateClient();
        var request = new HttpRequestMessage(method, url);
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
            (await response.Content.ReadAsStringAsync()).ShouldBe("""{"data":{"hello":"world"}}""");
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
    [InlineData("GET", "/graphql?query={hello}")]
    [InlineData("POST", "/graphql")]
    [InlineData("OPTIONS", "/graphql")]
    public async Task NoCorsConfig(string httpMethod, string url)
    {
        var ret = await ExecuteEndpoint(
            httpMethod == "POST" ? HttpMethod.Post : httpMethod == "OPTIONS" ? HttpMethod.Options : httpMethod == "GET" ? HttpMethod.Get : throw new ArgumentOutOfRangeException(nameof(httpMethod)),
            configureCors: _ => { },
            configureCorsPolicy: _ => { },
            configureGraphQl: _ => { },
            configureGraphQlEndpoint: _ => { },
            configureHeaders: headers =>
            {
                headers.Add("Origin", "http://www.example.com");
            },
            url);

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
        var ret = await ExecuteEndpoint(
            httpMethod == "POST" ? HttpMethod.Post : httpMethod == "OPTIONS" ? HttpMethod.Options : throw new ArgumentOutOfRangeException(nameof(httpMethod)),
            configureCors: _ => { },
            configureCorsPolicy: b =>
            {
                b.AllowCredentials();
                b.WithOrigins("http://www.example.com", "http://www.example2.com");
            },
            configureGraphQl: _ => { },
            configureGraphQlEndpoint: _ => { },
            configureHeaders: headers =>
            {
                headers.Add("Origin", pass ? "http://www.example.com" : "http://www.dummy.com");
            },
            "/graphql");

        ret.AllowHeaders.ShouldBeNull();
        ret.AllowMethods.ShouldBeNull();
        if (pass)
        {
            ret.AllowCredentials.ShouldBe(true);
            ret.AllowOrigin.ShouldBe("http://www.example.com");
        }
        else
        {
            ret.AllowCredentials.ShouldBeNull();
            ret.AllowOrigin.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("POST", true)]
    [InlineData("POST", false)]
    [InlineData("OPTIONS", true)]
    [InlineData("OPTIONS", false)]
    public async Task CustomPolicy(string httpMethod, bool pass)
    {
        var ret = await ExecuteEndpoint(
            httpMethod == "POST" ? HttpMethod.Post : httpMethod == "OPTIONS" ? HttpMethod.Options : throw new ArgumentOutOfRangeException(nameof(httpMethod)),
            configureCors: opts =>
            {
                opts.AddPolicy("MyCorsPolicy", b =>
                {
                    b.AllowCredentials();
                    b.WithOrigins("http://www.example.com", "http://www.example2.com");
                });
            },
            configureCorsPolicy: null,
            configureGraphQl: _ => { },
            configureGraphQlEndpoint: ep => ep.RequireCors("MyCorsPolicy"),
            configureHeaders: headers =>
            {
                headers.Add("Origin", pass ? "http://www.example.com" : "http://www.dummy.com");
            },
            "/graphql");

        ret.AllowHeaders.ShouldBeNull();
        ret.AllowMethods.ShouldBeNull();
        if (pass)
        {
            ret.AllowCredentials.ShouldBe(true);
            ret.AllowOrigin.ShouldBe("http://www.example.com");
        }
        else
        {
            ret.AllowCredentials.ShouldBeNull();
            ret.AllowOrigin.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("POST", true)]
    [InlineData("POST", false)]
    [InlineData("OPTIONS", true)]
    [InlineData("OPTIONS", false)]
    public async Task CustomOverrideDefaultPolicy(string httpMethod, bool pass)
    {
        var ret = await ExecuteEndpoint(
            httpMethod == "POST" ? HttpMethod.Post : httpMethod == "OPTIONS" ? HttpMethod.Options : throw new ArgumentOutOfRangeException(nameof(httpMethod)),
            configureCors: opts =>
            {
                opts.AddPolicy("MyCorsPolicy", b =>
                {
                    b.AllowCredentials();
                    b.WithOrigins("http://www.example.com", "http://www.example2.com");
                });
                opts.AddDefaultPolicy(b =>
                {
                    b.WithOrigins("http://www.alternate.com");
                });
            },
            configureCorsPolicy: null,
            configureGraphQl: _ => { },
            configureGraphQlEndpoint: ep => ep.RequireCors("MyCorsPolicy"),
            configureHeaders: headers =>
            {
                headers.Add("Origin", pass ? "http://www.example.com" : "http://www.dummy.com");
            },
            "/graphql");

        ret.AllowHeaders.ShouldBeNull();
        ret.AllowMethods.ShouldBeNull();
        if (pass)
        {
            ret.AllowCredentials.ShouldBe(true);
            ret.AllowOrigin.ShouldBe("http://www.example.com");
        }
        else
        {
            ret.AllowCredentials.ShouldBeNull();
            ret.AllowOrigin.ShouldBeNull();
        }
    }
}

#endif

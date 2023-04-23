using System.Net;
using Samples.Tests;

namespace Samples.Authorization.Tests;

public class EndToEndTests
{
    private const string SUCCESS_QUERY = "{hello}";
    private const string SUCCESS_RESPONSE = """{"data":{"hello":"Hello anybody."}}""";
    private const string ACCESS_DENIED_QUERY = "{helloUser}";
    private const string ACCESS_DENIED_ERRORS = """[{"message":"Access denied for field \u0027helloUser\u0027 on type \u0027Query\u0027.","locations":[{"line":1,"column":2}],"extensions":{"code":"ACCESS_DENIED","codes":["ACCESS_DENIED"]}}]""";
    private const string ACCESS_DENIED_RESPONSE = @"{""errors"":" + ACCESS_DENIED_ERRORS + "}";

    [Fact]
    public Task Playground()
        => new ServerTests<Program>().VerifyPlaygroundAsync("/ui/graphql");

    [Fact]
    public Task GraphQLGet_Success()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/graphql", SUCCESS_QUERY, SUCCESS_RESPONSE);

    [Fact]
    public Task GraphQLGet_AccessDenied()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/graphql", ACCESS_DENIED_QUERY, ACCESS_DENIED_RESPONSE, HttpStatusCode.BadRequest);

    [Fact]
    public Task GraphQLPost_Success()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/graphql", SUCCESS_QUERY, SUCCESS_RESPONSE);

    [Fact]
    public Task GraphQPost_AccessDenied()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/graphql", ACCESS_DENIED_QUERY, ACCESS_DENIED_RESPONSE, HttpStatusCode.BadRequest);

    [Fact]
    public Task GraphQLWebSocket_Success()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync("/graphql", SUCCESS_QUERY, SUCCESS_RESPONSE);

    [Fact]
    public Task GraphQLWebSocket_AccessDenied()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync("/graphql", ACCESS_DENIED_QUERY, ACCESS_DENIED_ERRORS, false);
}

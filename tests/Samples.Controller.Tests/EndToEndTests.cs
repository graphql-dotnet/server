using Samples.Tests;

namespace Samples.Controller.Tests;

public class EndToEndTests
{
    [Fact]
    public Task GraphiQL()
        => new ServerTests<Program>().VerifyGraphiQLAsync();

    [Fact]
    public Task GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/home/graphql");

    [Fact]
    public Task GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/home/graphql");

    [Fact]
    public Task GraphQLWebSocket()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync("/home/graphql");

    [Fact]
    public Task GraphQL2Get()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/home/graphql2");

    [Fact]
    public Task GraphQL2Post()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/home/graphql2");
}

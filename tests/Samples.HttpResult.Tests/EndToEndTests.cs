using Samples.Tests;

namespace Samples.Controller.Tests;

public class EndToEndTests
{
    [Fact]
    public Task GraphiQL()
        => new ServerTests<Program>().VerifyGraphiQLAsync();

    [Fact]
    public Task GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/graphql");

    [Fact]
    public Task GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/graphql");

    [Fact]
    public Task GraphQLWebSocket()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync("/graphql");

    [Fact]
    public Task GraphQLResultPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/graphql-result");
}

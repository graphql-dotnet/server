using Samples.Tests;

namespace Samples.EndpointRouting.Tests;

public class EndToEndTests
{
    [Fact]
    public Task Playground()
        => new ServerTests<Program>().VerifyPlaygroundAsync();

    [Fact]
    public Task GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync();

    [Fact]
    public Task GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync();

    [Fact]
    public Task GraphQLWebSocket()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync();
}

using Samples.Tests;

namespace Samples.Pages.Tests;

public class EndToEndTests
{
    [Fact]
    public Task GraphiQL()
        => new ServerTests<Program>().VerifyGraphiQLAsync();

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

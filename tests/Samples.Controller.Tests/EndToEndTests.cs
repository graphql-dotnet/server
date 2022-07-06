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
}

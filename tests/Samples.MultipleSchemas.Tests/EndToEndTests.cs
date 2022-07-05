using Samples.Tests;

namespace Samples.MultipleSchemas.Tests;

public class EndToEndTests
{
    [Fact]
    public Task Cats_Playground()
        => new ServerTests<Program>().VerifyPlaygroundAsync("/cats");

    [Fact]
    public Task Dogs_Playground()
        => new ServerTests<Program>().VerifyPlaygroundAsync("/dogs");

    [Fact]
    public Task Cats_GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/cats/graphql", @"{ cat(id:""1"") { name } }", @"{""data"":{""cat"":{""name"":""Fluffy""}}}");

    [Fact]
    public Task Cats_GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/cats/graphql", @"{ cat(id:""1"") { name } }", @"{""data"":{""cat"":{""name"":""Fluffy""}}}");

    [Fact]
    public Task Dogs_GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/dogs/graphql", @"{ dog(id:""1"") { name } }", @"{""data"":{""dog"":{""name"":""Shadow""}}}");

    [Fact]
    public Task Dogs_GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/dogs/graphql", @"{ dog(id:""1"") { name } }", @"{""data"":{""dog"":{""name"":""Shadow""}}}");
}

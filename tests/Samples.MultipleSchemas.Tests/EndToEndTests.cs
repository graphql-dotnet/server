using Samples.Tests;

namespace Samples.MultipleSchemas.Tests;

public class EndToEndTests
{
    private const string CAT_QUERY = """{ cat(id:"1") { name } }""";
    private const string CAT_RESPONSE = """{"data":{"cat":{"name":"Fluffy"}}}""";
    private const string DOG_QUERY = """{ dog(id:"1") { name } }""";
    private const string DOG_RESPONSE = """{"data":{"dog":{"name":"Shadow"}}}""";

    [Fact]
    public Task Cats_GraphiQL()
        => new ServerTests<Program>().VerifyGraphiQLAsync("/cats/ui/graphiql");

    [Fact]
    public Task Dogs_GraphiQL()
        => new ServerTests<Program>().VerifyGraphiQLAsync("/dogs/ui/graphiql");

    [Fact]
    public Task Cats_GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/cats/graphql", CAT_QUERY, CAT_RESPONSE);

    [Fact]
    public Task Cats_GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/cats/graphql", CAT_QUERY, CAT_RESPONSE);

    [Fact]
    public Task Cats_GraphQLWebSockets()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync("/cats/graphql", CAT_QUERY, CAT_RESPONSE);

    [Fact]
    public Task Dogs_GraphQLGet()
        => new ServerTests<Program>().VerifyGraphQLGetAsync("/dogs/graphql", DOG_QUERY, DOG_RESPONSE);

    [Fact]
    public Task Dogs_GraphQLPost()
        => new ServerTests<Program>().VerifyGraphQLPostAsync("/dogs/graphql", DOG_QUERY, DOG_RESPONSE);

    [Fact]
    public Task Dogs_GraphQLWebSockets()
        => new ServerTests<Program>().VerifyGraphQLWebSocketsAsync("/dogs/graphql", DOG_QUERY, DOG_RESPONSE);
}

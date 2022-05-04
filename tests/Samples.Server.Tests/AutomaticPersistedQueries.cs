using GraphQL.Transport;

namespace Samples.Server.Tests;

public class AutomaticPersistedQueries : BaseTest
{
    [Fact]
    public async Task Request_Without_Query_And_Hash_Should_Return_Error()
    {
        var response = await SendRequestAsync(new GraphQLRequest(), RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(@"{""errors"":[{""message"":""GraphQL query is missing.""}]}");
    }

    [Fact]
    public async Task Not_Persisted_Query_Should_Return_Not_Found_Code()
    {
        var extentions = new GraphQL.Inputs(new Dictionary<string, object> { ["hash"] = "1" });
        var response = await SendRequestAsync(new GraphQLRequest { Extensions = extentions }, RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(@"{""errors"":[{""message"":""Persisted query with '1' hash was not found."",""extensions"":{""code"":""PERSISTED_QUERY_NOT_FOUND"",""codes"":[""PERSISTED_QUERY_NOT_FOUND""]}}]}");
    }

    [Fact]
    public async Task Persisted_Query_Should_Work()
    {
        var extentions = new GraphQL.Inputs(new Dictionary<string, object> { ["hash"] = "2" });
        await SendRequestAsync(new GraphQLRequest { Query = "{ messages { content } }", Extensions = extentions }, RequestType.PostWithJson);

        var response = await SendRequestAsync(new GraphQLRequest { Extensions = extentions }, RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(@"{""data"":{""messages"":[]}}");
    }
}

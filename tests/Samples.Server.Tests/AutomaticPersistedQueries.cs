using GraphQL;
using GraphQL.Server.Transports.AspNetCore;
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
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "1"
            }
        });
        var response = await SendRequestAsync(new GraphQLRequest { Extensions = extentions }, RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(@"{""errors"":[{""message"":""Persisted query with '1' hash was not found."",""extensions"":{""code"":""PERSISTED_QUERY_NOT_FOUND"",""codes"":[""PERSISTED_QUERY_NOT_FOUND""]}}]}");
    }

    [Fact]
    public async Task Bad_Hash_Should_Be_Detected()
    {
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = "badHash"
            }
        });
        var response = await SendRequestAsync(new GraphQLRequest { Query = "{ messages { content } }", Extensions = extentions }, RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(@"{""errors"":[{""message"":""The 'badHash' hash doesn't correspond to a query."",""extensions"":{""code"":""PERSISTED_QUERY_BAD_HASH"",""codes"":[""PERSISTED_QUERY_BAD_HASH""]}}]}");
    }

    [Fact]
    public async Task Persisted_Query_Should_Work()
    {
        var query = "{ messages { content } }";
        var extentions = new Inputs(new Dictionary<string, object>
        {
            ["persistedQuery"] = new Dictionary<string, object>
            {
                ["sha256Hash"] = AutomaticPersistedQueryCache.ComputeSHA256(query)
            }
        });

        var expectedResult = @"{""data"":{""messages"":[]}}";
        var response = await SendRequestAsync(new GraphQLRequest { Query = query, Extensions = extentions }, RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(expectedResult);

        response = await SendRequestAsync(new GraphQLRequest { Extensions = extentions }, RequestType.PostWithJson);
        response.ShouldBeEquivalentJson(expectedResult);
    }
}

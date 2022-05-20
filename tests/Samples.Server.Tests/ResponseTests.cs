using System.Net;
using System.Text;
using GraphQL.Server;
using GraphQL.Transport;

namespace Samples.Server.Tests;

public class ResponseTests : BaseTest
{
    [Theory]
    [InlineData(RequestType.Get)]
    [InlineData(RequestType.PostWithJson)]
    [InlineData(RequestType.PostWithGraph)]
    [InlineData(RequestType.PostWithForm)]
    public async Task Single_Query_Should_Return_Single_Result(RequestType requestType)
    {
        var request = new GraphQLRequest { Query = "{ __schema { queryType { name } } }" };
        string response = await SendRequestAsync(request, requestType);
        response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
    }

    /// <summary>
    /// Tests that POST type requests are overridden by query string params.
    /// </summary>
    [Theory]
    [InlineData(RequestType.PostWithJson)]
    [InlineData(RequestType.PostWithGraph)]
    [InlineData(RequestType.PostWithForm)]
    public async Task Middleware_Should_Prioritise_Query_String_Values(RequestType requestType)
    {
        var request = new GraphQLRequest
        {
            Query = "mutation one ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
            Variables = @"{ ""content"": ""one content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs(),
            OperationName = "one"
        };

        var requestB = new GraphQLRequest
        {
            Query = "mutation two ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
            Variables = @"{ ""content"": ""two content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs(),
            OperationName = "two"
        };

        string response = await SendRequestAsync(request, requestType, queryStringOverride: requestB);
        response.ShouldBeEquivalentJson(
            @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01T00:00:00Z"",""content"":""two content"",""from"":{""id"":""1""}}}}",
            ignoreExtensions: true);
    }

    [Fact]
    public async Task Batched_Query_Should_Return_Multiple_Results()
    {
        string response = await SendBatchRequestAsync(
            new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" },
            new GraphQLRequest { Query = "query two { __schema { queryType { name } } }", OperationName = "two" },
            new GraphQLRequest { Query = "query three { __schema { queryType { name } } }", OperationName = "three" }
            );
        response.ShouldBeEquivalentJson(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]", ignoreExtensions: true);
    }

    [Fact]
    public async Task Batched_Query_Should_Return_Single_Result_As_Array()
    {
        string response = await SendBatchRequestAsync(
            new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" }
            );
        response.ShouldBeEquivalentJson(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]", ignoreExtensions: true);
    }

    [Fact]
    public async Task Mutation_For_Get_Fails()
    {
        var response = await SendRequestAsync(new GraphQLRequest { Query = "mutation { __typename }" }, RequestType.Get);

        response.ShouldBe(@"{""errors"":[{""message"":""Only query operations allowed for GET requests."",""locations"":[{""line"":1,""column"":1}],""extensions"":{""code"":""HTTP_METHOD_VALIDATION"",""codes"":[""HTTP_METHOD_VALIDATION""]}}]}");
    }

    [Theory]
    [MemberData(nameof(WrongQueryData))]
    public async Task Wrong_Query_Should_Return_Error(HttpMethod httpMethod, HttpContent httpContent,
        HttpStatusCode expectedStatusCode, string expected)
    {
        var response = await SendRequestAsync(httpMethod, httpContent);

        response.StatusCode.ShouldBe(expectedStatusCode);

        string content = await response.Content.ReadAsStringAsync();
        if (expected == null)
            content.ShouldBe("");
        else
            content.ShouldBeEquivalentJson(expected);
    }

    public static IEnumerable<object[]> WrongQueryData => new object[][]
    {
        // Methods other than GET or POST shouldn't be allowed
        new object[]
        {
            HttpMethod.Put,
            new StringContent(Serializer.ToJson(new GraphQLRequest { Query = "query { __schema { queryType { name } } }" }), Encoding.UTF8, "application/json"),
            HttpStatusCode.NotFound,
            null,
        },

        // POST with unsupported mime type should be a unsupported media type
        new object[]
        {
            HttpMethod.Post,
            new StringContent(Serializer.ToJson(new GraphQLRequest { Query = "query { __schema { queryType { name } } }" }), Encoding.UTF8, "something/unknown"),
            HttpStatusCode.UnsupportedMediaType,
            @"{""errors"":[{""message"":""Invalid 'Content-Type' header: non-supported media type 'something/unknown; charset=utf-8'. Must be 'application/json', 'application/graphql' or a form body."",""extensions"":{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}]}"
        },

        // MediaTypeHeaderValue ctor throws exception
        // POST with unsupported charset should be a unsupported media type
        //new object[]
        //{
        //    HttpMethod.Post,
        //    new StringContent(Serializer.ToJson(new GraphQLRequest { Query = "query { __schema { queryType { name } } }" }), Encoding.UTF8, "application/json; charset=utf-3"),
        //    HttpStatusCode.UnsupportedMediaType,
        //    @"{""errors"":[{""message"":""Invalid 'Content-Type' header: non-supported media type 'application/json; charset=utf-3'. Must be 'application/json', 'application/graphql' or a form body."",""extensions"":{""code"":""INVALID_CONTENT_TYPE"",""codes"":[""INVALID_CONTENT_TYPE""]}}]}"
        //},

        // POST with JSON mime type that doesn't start with an object or array token should be a bad request
        new object[]
        {
            HttpMethod.Post,
            new StringContent("Oops", Encoding.UTF8, "application/json"),
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""JSON body text could not be parsed. 'O' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}"
        },

        // POST with JSON mime type that is invalid JSON should be a bad request
        new object[]
        {
            HttpMethod.Post,
            new StringContent("{oops}", Encoding.UTF8, "application/json"),
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""JSON body text could not be parsed. 'o' is an invalid start of a property name. Expected a '""'. Path: $ | LineNumber: 0 | BytePositionInLine: 1."",""extensions"":{""code"":""JSON_INVALID"",""codes"":[""JSON_INVALID""]}}]}"
        },

        // POST with JSON mime type that is null JSON should be a bad request
        new object[]
        {
            HttpMethod.Post,
            new StringContent("null", Encoding.UTF8, "application/json"),
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}"
        },

        // GET with an empty QueryString should be a bad request
        new object[]
        {
            HttpMethod.Get,
            null,
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""GraphQL query is missing."",""extensions"":{""code"":""QUERY_MISSING"",""codes"":[""QUERY_MISSING""]}}]}"
        },

        // POST with a GraphQL parsing error should be a bad request
        new object[]
        {
            HttpMethod.Post,
            new StringContent(@"{""query"":""parseError""}", Encoding.UTF8, "application/json"),
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""Error parsing query: Expected \u0022query/mutation/subscription/fragment/schema/scalar/type/interface/union/enum/input/extend/directive\u0022, found Name \u0022parseError\u0022"",""locations"":[{""line"":1,""column"":1}],""extensions"":{""code"":""SYNTAX_ERROR"",""codes"":[""SYNTAX_ERROR""]}}]}"
        },

        // POST with no operation should be a bad request
        new object[]
        {
            HttpMethod.Post,
            new StringContent(@"{""query"":""fragment frag on Query { hello }""}", Encoding.UTF8, "application/json"),
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""Document does not contain any operations."",""extensions"":{""code"":""NO_OPERATION"",""codes"":[""NO_OPERATION""]}}]}"
        },

        // POST with validation error should be a bad request
        new object[]
        {
            HttpMethod.Post,
            new StringContent(@"{""query"":""{ dummy }""}", Encoding.UTF8, "application/json"),
            HttpStatusCode.BadRequest,
            @"{""errors"":[{""message"":""Cannot query field \u0027dummy\u0027 on type \u0027ChatQuery\u0027."",""locations"":[{""line"":1,""column"":3}],""extensions"":{""code"":""FIELDS_ON_CORRECT_TYPE"",""codes"":[""FIELDS_ON_CORRECT_TYPE""],""number"":""5.3.1""}}]}"
        },
    };

    [Theory]
    [InlineData(RequestType.PostWithJson)]
    [InlineData(RequestType.PostWithGraph)]
    [InlineData(RequestType.PostWithForm)]
    public async Task Serializer_Should_Handle_Inline_Variables(RequestType requestType)
    {
        var request = new GraphQLRequest
        {
            Query = @"mutation { addMessage(message: { content: ""some content"", fromId: ""1"", sentAt: ""2020-01-01"" }) { sentAt, content, from { id } } }"
        };
        string response = await SendRequestAsync(request, requestType);
        response.ShouldBeEquivalentJson(
            @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01T00:00:00Z"",""content"":""some content"",""from"":{""id"":""1""}}}}",
            ignoreExtensions: true);
    }

    [Theory]
    [InlineData(RequestType.PostWithJson)]
    [InlineData(RequestType.PostWithGraph)]
    [InlineData(RequestType.PostWithForm)]
    public async Task Serializer_Should_Handle_Variables(RequestType requestType)
    {
        var request = new GraphQLRequest
        {
            Query = "mutation ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
            Variables = @"{ ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs()
        };
        string response = await SendRequestAsync(request, requestType);
        response.ShouldBeEquivalentJson(
            @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01T00:00:00Z"",""content"":""some content"",""from"":{""id"":""1""}}}}",
            ignoreExtensions: true);
    }

    [Theory]
    [InlineData(RequestType.PostWithJson)]
    [InlineData(RequestType.PostWithGraph)]
    [InlineData(RequestType.PostWithForm)]
    public async Task Serializer_Should_Handle_Complex_Variable(RequestType requestType)
    {
        var request = new GraphQLRequest
        {
            Query = "mutation ($msg: MessageInputType!) { addMessage(message: $msg) { sentAt, content, from { id } } }",
            Variables = @"{ ""msg"": { ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" } }".ToInputs()
        };
        string response = await SendRequestAsync(request, requestType);
        response.ShouldBeEquivalentJson(
            @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01T00:00:00Z"",""content"":""some content"",""from"":{""id"":""1""}}}}",
            ignoreExtensions: true);
    }

    [Theory]
    [InlineData(RequestType.Get)]
    [InlineData(RequestType.PostWithJson)]
    [InlineData(RequestType.PostWithGraph)]
    [InlineData(RequestType.PostWithForm)]
    public async Task Serializer_Should_Handle_Empty_Variables(RequestType requestType)
    {
        var request = new GraphQLRequest
        {
            Query = "{ __schema { queryType { name } } }",
            Variables = "{}".ToInputs()
        };
        string response = await SendRequestAsync(request, requestType);
        response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
    }
}

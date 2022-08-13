using System.Net;
using System.Text.Json;
using GraphQL.Transport;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Samples.Tests;

namespace Samples.Jwt.Tests;

public class EndToEndTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _applicationFactory;
    private readonly TestServer _testServer;
    private readonly HttpClient _testClient;
    private const string ACCESS_DENIED_RESPONSE = @"{""errors"":[{""message"":""Access denied for schema."",""extensions"":{""code"":""ACCESS_DENIED"",""codes"":[""ACCESS_DENIED""]}}]}";

    public EndToEndTests()
    {
        _applicationFactory = new WebApplicationFactory<Program>();
        _testServer = _applicationFactory.Server;
        _testClient = _testServer.CreateClient();
    }

    public void Dispose()
    {
        _testClient.Dispose();
        _testServer.Dispose();
        _applicationFactory.Dispose();
    }

    [Fact]
    public Task GraphiQL()
        => _testServer.VerifyGraphiQLAsync();

    [Fact]
    public async Task GraphQLGet_Authorized()
    {
        var token = await GetJwtToken();
        await _testServer.VerifyGraphQLGetAsync(jwtToken: token);
    }

    [Fact]
    public async Task GraphQLGet_Unauthorized()
    {
        await _testServer.VerifyGraphQLGetAsync(expected: ACCESS_DENIED_RESPONSE, statusCode: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GraphQLPost_Authorized()
    {
        var token = await GetJwtToken();
        await _testServer.VerifyGraphQLPostAsync(jwtToken: token);
    }

    [Fact]
    public async Task GraphQLPost_Unauthorized()
    {
        await _testServer.VerifyGraphQLPostAsync(expected: ACCESS_DENIED_RESPONSE, statusCode: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GraphQLWebSocket_Authorized_HttpHeader()
    {
        var token = await GetJwtToken();
        await _testServer.VerifyGraphQLWebSocketsAsync(authHeaderJwtToken: token);
    }

    [Fact]
    public async Task GraphQLWebSocket_Authorized_WebSocketPayload()
    {
        var token = await GetJwtToken();
        await _testServer.VerifyGraphQLWebSocketsAsync(payloadJwtToken: token);
    }

    [Fact]
    public async Task GraphQLWebSocket_Unauthorized()
    {
        var webSocketClient = _testServer.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
        {
            request.Headers["Sec-WebSocket-Protocol"] = "graphql-transport-ws";
        };
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var webSocket = await webSocketClient.ConnectAsync(new Uri(_testServer.BaseAddress, "/graphql"), default);

        // send CONNECTION_INIT
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "connection_init",
        });

        // wait for connection rejection
        await webSocket.ReceiveCloseMessageAsync();
    }

    [Fact]
    public Task OAuth2_Get()
        => GetJwtToken();

    [Fact]
    public async Task OAuth2_Post_UrlEncoded()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/token");
        var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
        {
            new("grant_type", "client_credentials"),
            new("client_id", "sampleClientId"),
            new("client_secret", "sampleSecret"),
        });
        request.Content = content;
        using var response = await _testClient.SendAsync(request);
        await ProcessOAuthResponseAsync(response);
    }

    [Fact]
    public async Task OAuth2_Post_MultipartFormData()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/token");
        var content = new MultipartFormDataContent()
        {
            { new StringContent("client_credentials"), "grant_type" },
            { new StringContent("sampleClientId"), "client_id" },
            { new StringContent("sampleSecret"), "client_secret" },
        };
        request.Content = content;
        using var response = await _testClient.SendAsync(request);
        await ProcessOAuthResponseAsync(response);
    }

    private async Task<string> GetJwtToken()
    {
        using var response = await _testClient.GetAsync("/token?grant_type=client_credentials&client_id=sampleClientId&client_secret=sampleSecret");
        return await ProcessOAuthResponseAsync(response);
    }

    private async Task<string> ProcessOAuthResponseAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var str = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<OAuthResponse>(str);
        authResponse.ShouldNotBeNull();
        authResponse.access_token.ShouldNotBeNull();
        authResponse.expires_in.ShouldBe(300); // 300 seconds / 5 minutes
        authResponse.token_type.ShouldBe("Bearer");
        return authResponse.access_token;
    }

    private class OAuthResponse
    {
        public string? token_type { get; set; }
        public string? access_token { get; set; }
        public int? expires_in { get; set; }
    }
}

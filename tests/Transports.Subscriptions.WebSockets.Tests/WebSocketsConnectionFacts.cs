using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class WebSocketsConnectionFacts
    {
        private readonly WebApplicationFactory<TestStartup> _factory;

        public WebSocketsConnectionFacts(WebApplicationFactory<TestStartup> factory)
        {
            _factory = factory;
        }

        private Task<WebSocket> ConnectAsync(string protocol)
        {
            var client = _factory.Server.CreateWebSocketClient();
            client.ConfigureRequest = request => { request.Headers.Add("Sec-WebSocket-Protocol", protocol); };
            return client.ConnectAsync(new Uri("http://localhost/graphql"), CancellationToken.None);
        }

        [Fact]
        public async Task should_accept_websocket_connection()
        {
            /* Given */
            /* When */
            var socket = await ConnectAsync("graphql-ws").ConfigureAwait(false);

            /* Then */
            Assert.Equal(WebSocketState.Open, socket.State);
        }

        [Fact]
        public async Task should_not_accept_websocket_with_wrong_protocol()
        {
            /* Given */
            /* When */
            var socket = await ConnectAsync("do-not-accept").ConfigureAwait(false);
            var segment = new ArraySegment<byte>(new byte[1024]);
            var received = await socket.ReceiveAsync(segment, CancellationToken.None).ConfigureAwait(false);

            /* Then */
            Assert.Equal(WebSocketCloseStatus.ProtocolError, received.CloseStatus);
        }
    }
}
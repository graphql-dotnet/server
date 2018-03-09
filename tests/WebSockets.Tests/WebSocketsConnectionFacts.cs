using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class WebSocketsConnectionFacts
    {
        public WebSocketsConnectionFacts()
        {
            _server = new TestServer(WebHost
                .CreateDefaultBuilder()
                .UseStartup<TestStartup>());
        }

        private readonly TestServer _server;

        private Task<WebSocket> ConnectAsync(string protocol)
        {
            var client = _server.CreateWebSocketClient();
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
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
    public class AuthenticatedWebSocketsConnectionFacts
    {
        public AuthenticatedWebSocketsConnectionFacts()
        {
            _server = new TestServer(WebHost
                .CreateDefaultBuilder()
                .UseStartup<AuthenticatedTestStartup>());
        }

        private readonly TestServer _server;

        private Task<WebSocket> ConnectAsync(string protocol, bool authenticate)
        {
            var client = _server.CreateWebSocketClient();
            client.ConfigureRequest = request =>
            {
                request.Headers.Add("Sec-WebSocket-Protocol", protocol);
                
                if (authenticate)
                {
                    request.AddClaimHeader("sub", "1");
                }
            };

            return client.ConnectAsync(new Uri("http://localhost/graphql"), CancellationToken.None);
        }

        [Fact]
        public async Task should_accept_authenticated_connection()
        {
            
            /* Given */
            /* When */
            var socket = await ConnectAsync("graphql-ws", true).ConfigureAwait(false);

            /* Then */
            Assert.Equal(WebSocketState.Open, socket.State);
        }

        [Fact]
        public async Task should_reject_unauthenticated_connection()
        {
            /* Given */
            /* When */

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var socket = await ConnectAsync("graphql-ws", false).ConfigureAwait(false);
            });

            /* Then */
            Assert.Contains("401", ex.InnerException.Message);
        }
    }
}
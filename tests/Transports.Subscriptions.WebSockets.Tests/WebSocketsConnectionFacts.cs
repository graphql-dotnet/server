using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Shouldly;

#if NETFRAMEWORK || NETCOREAPP2_2
using Microsoft.AspNetCore;
#else
using Microsoft.Extensions.Hosting;
#endif

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class WebSocketsConnectionFacts : IDisposable
    {
#if NETFRAMEWORK || NETCOREAPP2_2
        public WebSocketsConnectionFacts()
        {
            _server = new TestServer(WebHost
                .CreateDefaultBuilder()
                .UseStartup<TestStartup>());
        }

        private readonly TestServer _server;
#else
        public WebSocketsConnectionFacts()
        {
            _host = Host
                .CreateDefaultBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseTestServer()
                        .UseStartup<TestStartup>();
                })
                .Start();

            _server = _host.GetTestServer();
        }

        private readonly IHost _host;
        private readonly TestServer _server;
#endif

        private Task<WebSocket> ConnectAsync(string protocol)
        {
            var client = _server.CreateWebSocketClient();
            client.ConfigureRequest = request => request.Headers.Add("Sec-WebSocket-Protocol", protocol);
            return client.ConnectAsync(new Uri("http://localhost/graphql"), CancellationToken.None);
        }

        [Fact]
        public async Task Should_accept_websocket_connection()
        {
            /* Given */
            /* When */
            var socket = await ConnectAsync("graphql-ws");

            /* Then */
            Assert.Equal(WebSocketState.Open, socket.State);
        }

        [Fact]
        public async Task Should_not_accept_websocket_with_wrong_protocol()
        {
            /* Given */
            /* When */
            var socket = await ConnectAsync("do-not-accept");
            var segment = new ArraySegment<byte>(new byte[1024]);
            var received = await socket.ReceiveAsync(segment, CancellationToken.None);

            /* Then */
            received.CloseStatus.ShouldBe(WebSocketCloseStatus.ProtocolError);
        }

        public void Dispose()
        {
            _server.Dispose();
#if !(NETFRAMEWORK || NETCOREAPP2_2)
            _host.Dispose();
#endif
        }
    }
}

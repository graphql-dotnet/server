using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;
using NSubstitute;
using Xunit;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class GraphQLEndPointFacts
    {
        [Fact]
        public async Task should_connect()
        {
            /* Given */
            var log = Substitute.For<ILogger<GraphQlEndPoint<TestSchema>>>();
            var handler = Substitute.For<ISubscriptionProtocolHandler<TestSchema>>();
            var connection = Substitute.For<IConnectionContext>();
            connection.ConnectionId.Returns("1");

            var sut = new GraphQlEndPoint<TestSchema>(handler, log);
                
            /* When */
            await sut.OnConnectedAsync(connection).ConfigureAwait(false);

            /* Then */
            Assert.True(sut.Connections.ContainsKey(connection.ConnectionId));
        }

        [Fact]
        public async Task should_receive_messages()
        {
            /* Given */
            var log = Substitute.For<ILogger<GraphQlEndPoint<TestSchema>>>();
            var handler = Substitute.For<ISubscriptionProtocolHandler<TestSchema>>();
            var connection = Substitute.For<IConnectionContext>();
            connection.ConnectionId.Returns("1");

            var sut = new GraphQlEndPoint<TestSchema>(handler, log);

            /* When */
            await sut.OnConnectedAsync(connection).ConfigureAwait(false);

            /* Then */
            await connection.Reader.Received().ReadMessageAsync<OperationMessage>().ConfigureAwait(false);
        }

        [Fact]
        public async Task should_handle_received_messages()
        {
            /* Given */
            var log = Substitute.For<ILogger<GraphQlEndPoint<TestSchema>>>();
            var handler = Substitute.For<ISubscriptionProtocolHandler<TestSchema>>();
            var connection = Substitute.For<IConnectionContext>();
            connection.ConnectionId.Returns("1");

            var message = new OperationMessage();
            connection.Reader.ReadMessageAsync<OperationMessage>().Returns(message)
                .AndDoes(ci => connection.CloseStatus.Returns(WebSocketCloseStatus.NormalClosure));
            var sut = new GraphQlEndPoint<TestSchema>(handler, log);

            /* When */
            await sut.OnConnectedAsync(connection).ConfigureAwait(false);

            /* Then */
            await handler.Received().HandleMessageAsync(
                Arg.Is<OperationMessageContext>(context => context.ConnectionId == connection.ConnectionId
                                                           && context.Op == message)).ConfigureAwait(false);
        }

        [Fact]
        public async Task should_disconnect()
        {
            /* Given */
            var log = Substitute.For<ILogger<GraphQlEndPoint<TestSchema>>>();
            var handler = Substitute.For<ISubscriptionProtocolHandler<TestSchema>>();
            var connection = Substitute.For<IConnectionContext>();
            connection.ConnectionId.Returns("1");

            var sut = new GraphQlEndPoint<TestSchema>(handler, log);

            /* When */
            await sut.OnConnectedAsync(connection).ConfigureAwait(false);
            await sut.CloseConnectionAsync(connection).ConfigureAwait(false);

            /* Then */
            Assert.False(sut.Connections.ContainsKey(connection.ConnectionId));
            await connection.Received().CloseAsync().ConfigureAwait(false);
        }
    }
}

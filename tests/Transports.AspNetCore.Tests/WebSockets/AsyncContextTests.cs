using System.Reactive.Subjects;

namespace Tests.WebSockets;

public class AsyncContextTests
{
    /*
     * This test ensures that the GraphQL execution of subscription events occurs
     * within the ExecutionContext of the client, not the sender of the subscription
     * event.  This is important because the resolvers may be using IHttpContextAccessor
     * and/or other ExecutionContext-bound services within the subscription event, and
     * if it was so, execution will not work as expected.
     * 
     * Even worse, it could mean that IHttpContextAccessor.HttpContext might return the
     * HttpContext of the sender of the event, rather than the one that initiated the
     * subscription, which can be unanticipated and very hard to diagnose.
     *
     * However, this was fixed within GraphQL.NET 7.3.0 by capturing the ExecutionContext
     * upon subscription and restoring it for data events.
     *
     * Note that this issue has nothing to do with DI service scope, as DI service scope
     * is not used by AsyncLocal or HttpContextAccessor.
     *
     * Note that since the IResolveFieldContext.User and UserContext properties do not
     * rely on AsyncLocal or IHttpContextAccessor for propagation, their use (and in turn,
     * the provided authorization rule) are unaffected by this issue.
     *
     */
    [Fact]
    public async Task EnsureCorrectAsyncContextWithinSubscriptionResolvers()
    {
        using var replaySubject = new ReplaySubject<Class1>();

        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
#if !NETCOREAPP3_1_OR_GREATER
                services.AddHostApplicationLifetime();
#endif
                services.AddSingleton(replaySubject);
                services.AddHttpContextAccessor();
                services.AddSingleton<Class1Type>();
                services.AddGraphQL(b => b
                    .AddAutoSchema<Query>(o => o.WithSubscription<Subscription>())
                    .AddSystemTextJson()
                    .AddErrorInfoProvider(o => o.ExposeExceptionDetails = true)
                    .ConfigureSchema(s =>
                    {
                        s.RegisterTypeMapping<Class1, Class1Type>();
                    })
                    .AddScopedSubscriptionExecutionStrategy()
                );
            })
            .Configure(app =>
            {
                app.UseWebSockets();
                app.UseGraphQL();
            })
        );

        // create websocket connection
        var webSocketClient = server.CreateWebSocketClient();
        webSocketClient.ConfigureRequest = request =>
        {
            request.Headers["Sec-WebSocket-Protocol"] = "graphql-transport-ws";
        };
        webSocketClient.SubProtocols.Add("graphql-transport-ws");
        using var webSocket = await webSocketClient.ConnectAsync(new Uri(server.BaseAddress, "graphql"), default);

        // send CONNECTION_INIT
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "connection_init"
        });

        // wait for CONNECTION_ACK
        var message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe("connection_ack");

        // subscribe
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "subscribe",
            Id = "123",
            Payload = new GraphQLRequest
            {
                Query = "subscription { events { hasHttpContext } }",
            },
        });

        // It is necessary to allow time for the asynchronous websocket handler code
        // to execute prior to the independent call to AddMessageInternal below.
        // Since the websocket call does not return a response when the subscription
        // has completed being set up, there is no response we can await to determine
        // when to call AddMessageInternal; so for the purposes of testing, we make
        // an additional call here.

        // wait for the message to be handled by the server;
        // just send a ping and wait for the pong
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "ping",
        });
        message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe("pong");

        // verify the subscription has connected (should be connected because of the ping)
        replaySubject.HasObservers.ShouldBeTrue();

        // post a new message
        replaySubject.OnNext(new Class1());

        // wait for a new message sent over this websocket
        message = await webSocket.ReceiveMessageAsync();
        message.Type.ShouldBe("next");
        message.Payload.ShouldBe("""{"data":{"events":{"hasHttpContext":true}}}""");

        // unsubscribe
        await webSocket.SendMessageAsync(new OperationMessage
        {
            Type = "complete",
            Id = "123",
        });

        // close websocket
        await webSocket.CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, null, default);

        // wait for websocket closure
        (await webSocket.ReceiveCloseAsync()).ShouldBe(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure);
    }

    public class Query
    {
        public static string Hero => throw new NotImplementedException();
    }

    public class Subscription
    {
        public static IObservable<Class1> Events([FromServices] IHttpContextAccessor accessor, [FromServices] ReplaySubject<Class1> replaySubject)
        {
            var context = accessor.HttpContext;
            context.ShouldNotBeNull(); // we can see that the http context is passed to the initial subscription resolver correctly
            return replaySubject;
        }
    }

    public class Class1Type : ObjectGraphType<Class1>
    {
        public Class1Type()
        {
            Field<bool>("HasHttpContext")
                .Resolve(context =>
                {
                    var accessor = context.RequestServices.ShouldNotBeNull().GetRequiredService<IHttpContextAccessor>();
                    // here we can see that the http context is accessible by field resolvers within a subscription data event
                    return accessor.HttpContext != null; // returns true for GraphQL.NET 7.3.0+
                });
        }
    }

    public class Class1
    {
    }
}

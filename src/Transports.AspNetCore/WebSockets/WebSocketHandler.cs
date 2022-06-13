using System.Net.WebSockets;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <inheritdoc cref="WebSocketHandler"/>
public class WebSocketHandler<TSchema> : WebSocketHandler, IWebSocketHandler<TSchema>
    where TSchema : ISchema
{
    /// <inheritdoc cref="WebSocketHandler(IGraphQLSerializer, IDocumentExecuter, IServiceScopeFactory, GraphQLWebSocketOptions, IAuthorizationOptions, IHostApplicationLifetime, IWebSocketAuthenticationService)"/>
    public WebSocketHandler(
        IGraphQLSerializer serializer,
        IDocumentExecuter<TSchema> executer,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLWebSocketOptions options,
        IAuthorizationOptions authorizationOptions,
        IHostApplicationLifetime hostApplicationLifetime,
        IWebSocketAuthenticationService? authorizationService)
        : base(serializer, executer, serviceScopeFactory, options, authorizationOptions, hostApplicationLifetime, authorizationService)
    {
    }

    /// <inheritdoc cref="WebSocketHandler(IGraphQLSerializer, IDocumentExecuter, IServiceScopeFactory, GraphQLWebSocketOptions, IAuthorizationOptions, IHostApplicationLifetime, IWebSocketAuthenticationService)"/>
    public WebSocketHandler(
        IGraphQLSerializer serializer,
        IDocumentExecuter<TSchema> executer,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLWebSocketOptions options,
        IAuthorizationOptions authorizationOptions,
        IHostApplicationLifetime hostApplicationLifetime)
        : base(serializer, executer, serviceScopeFactory, options, authorizationOptions, hostApplicationLifetime, null)
    {
    }
}

/// <inheritdoc cref="IWebSocketHandler"/>
public class WebSocketHandler : IWebSocketHandler
{
    /// <summary>
    /// Gets the <see cref="IGraphQLSerializer"/> instance used to serialize and deserialize <see cref="OperationMessage"/> messages.
    /// </summary>
    protected IGraphQLSerializer Serializer { get; }

    /// <summary>
    /// Gets the <see cref="IDocumentExecuter"/> instance used to execute GraphQL requests.
    /// </summary>
    protected IDocumentExecuter Executer { get; }

    /// <summary>
    /// Gets the service scope factory used to create a dependency injection service scope for each request.
    /// </summary>
    protected IServiceScopeFactory ServiceScopeFactory { get; }

    /// <summary>
    /// Gets the <see cref="IHostApplicationLifetime"/> instance that signals when the application is shutting down.
    /// </summary>
    protected IHostApplicationLifetime HostApplicationLifetime { get; }

    /// <summary>
    /// Gets the <see cref="IWebSocketAuthenticationService"/> instance used to authenticate connections,
    /// or <see langword="null"/> if none is configured.
    /// </summary>
    protected IWebSocketAuthenticationService? AuthorizationService { get; }

    /// <summary>
    /// Gets the configuration options for this instance.
    /// </summary>
    protected GraphQLWebSocketOptions Options { get; }

    /// <summary>
    /// Gets the authorization options for this instance.
    /// </summary>
    protected IAuthorizationOptions AuthorizationOptions { get; }

    private static readonly IEnumerable<string> _supportedSubProtocols = new List<string>(new[] {
        GraphQLWs.SubscriptionServer.SubProtocol,
        SubscriptionsTransportWs.SubscriptionServer.SubProtocol,
    }).AsReadOnly();

    /// <inheritdoc/>
    public virtual IEnumerable<string> SupportedSubProtocols => _supportedSubProtocols;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="serializer">The <see cref="IGraphQLSerializer"/> instance used to serialize and deserialize <see cref="OperationMessage"/> messages.</param>
    /// <param name="executer">The <see cref="IDocumentExecuter"/> instance used to execute GraphQL requests.</param>
    /// <param name="serviceScopeFactory">The service scope factory used to create a dependency injection service scope for each request.</param>
    /// <param name="options">Configuration options for this instance.</param>
    /// <param name="authorizationOptions">Authorization options for this instance.</param>
    /// <param name="hostApplicationLifetime">The <see cref="IHostApplicationLifetime"/> instance that signals when the application is shutting down.</param>
    /// <param name="authorizationService">An optional service to authorize connections.</param>
    public WebSocketHandler(
        IGraphQLSerializer serializer,
        IDocumentExecuter executer,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLWebSocketOptions options,
        IAuthorizationOptions authorizationOptions,
        IHostApplicationLifetime hostApplicationLifetime,
        IWebSocketAuthenticationService? authorizationService = null)
    {
        Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        Executer = executer ?? throw new ArgumentNullException(nameof(executer));
        ServiceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        AuthorizationOptions = authorizationOptions ?? throw new ArgumentNullException(nameof(authorizationOptions));
        HostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        AuthorizationService = authorizationService;
    }

    /// <inheritdoc/>
    public virtual async Task ExecuteAsync(HttpContext httpContext, WebSocket webSocket, string subProtocol, IUserContextBuilder userContextBuilder)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));
        if (webSocket == null)
            throw new ArgumentNullException(nameof(webSocket));
        if (subProtocol == null)
            throw new ArgumentNullException(nameof(subProtocol));
        if (userContextBuilder == null)
            throw new ArgumentNullException(nameof(userContextBuilder));
        var appStoppingToken = HostApplicationLifetime.ApplicationStopping;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, appStoppingToken);
        if (cts.Token.IsCancellationRequested)
            return;
        try
        {
            using var webSocketConnection = CreateWebSocketConnection(httpContext, webSocket, cts.Token);
            using var messageProcessor = CreateMessageProcessor(webSocketConnection, subProtocol, userContextBuilder);
            await webSocketConnection.ExecuteAsync(messageProcessor);
        }
        catch (OperationCanceledException) when (appStoppingToken.IsCancellationRequested)
        {
            // terminate all pending WebSockets connections when the application is in the process of stopping

            // note: we are consuming OCE in this case because ASP.NET Core does not consider the task as canceled when
            // an OCE occurs that is not due to httpContext.RequestAborted; so to prevent ASP.NET Core from considering
            // this a "regular" exception, we consume it here
        }
    }

    /// <summary>
    /// Creates an <see cref="IWebSocketConnection"/>, a WebSocket message pump.
    /// </summary>
    protected virtual IWebSocketConnection CreateWebSocketConnection(HttpContext httpContext, WebSocket webSocket, CancellationToken cancellationToken)
        => new WebSocketConnection(httpContext, webSocket, Serializer, Options, cancellationToken);

    /// <summary>
    /// Builds an <see cref="IOperationMessageProcessor"/> for the specified sub-protocol.
    /// </summary>
    protected virtual IOperationMessageProcessor CreateMessageProcessor(IWebSocketConnection webSocketConnection, string subProtocol, IUserContextBuilder userContextBuilder)
    {
        if (subProtocol == GraphQLWs.SubscriptionServer.SubProtocol)
        {
            return new GraphQLWs.SubscriptionServer(
                webSocketConnection,
                Options,
                AuthorizationOptions,
                Executer,
                Serializer,
                ServiceScopeFactory,
                userContextBuilder,
                AuthorizationService);
        }
        else if (subProtocol == SubscriptionsTransportWs.SubscriptionServer.SubProtocol)
        {
            return new SubscriptionsTransportWs.SubscriptionServer(
                webSocketConnection,
                Options,
                AuthorizationOptions,
                Executer,
                Serializer,
                ServiceScopeFactory,
                userContextBuilder,
                AuthorizationService);
        }

        throw new ArgumentOutOfRangeException(nameof(subProtocol));
    }
}


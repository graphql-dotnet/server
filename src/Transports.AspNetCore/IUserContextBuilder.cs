namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Creates a user context from a <see cref="HttpContext"/> and/or the WebSocket initialization message's payload.
/// <br/><br/>
/// The generated user context may be used for one or more GraphQL requests or
/// subscriptions over the same HTTP connection.
/// </summary>
public interface IUserContextBuilder
{
    /// <inheritdoc cref="IUserContextBuilder"/>
    /// <param name="context">The <see cref="HttpContext"/> of the HTTP connection.</param>
    /// <param name="payload">
    /// The payload of the WebSocket connection request, if applicable.  Typically this is either <see langword="null"/> or
    /// an object that has not fully been deserialized yet; when using the Newtonsoft.Json deserializer, this will be a JObject,
    /// and when using System.Text.Json this will be a JsonElement.  You may call
    /// <see cref="IGraphQLSerializer.ReadNode{T}(object?)"/> to deserialize the node into the expected type.  To deserialize
    /// into a nested set of <see cref="IDictionary{TKey, TValue}">IDictionary&lt;string, object?&gt;</see> maps, call
    /// <see cref="IGraphQLSerializer.ReadNode{T}(object?)"/> with <see cref="Inputs"/> as the generic type.
    /// <br/><br/>
    /// To determine if this is a WebSocket connection request, check
    /// <paramref name="context"/>.<see cref="HttpContext.WebSockets">WebSockets</see>.<see cref="WebSocketManager.IsWebSocketRequest">IsWebSocketRequest</see>.
    /// </param>
    /// <returns>Dictionary object representing user context. Return <see langword="null"/> to use default user context.</returns>
    ValueTask<IDictionary<string, object?>?> BuildUserContextAsync(HttpContext context, object? payload);
}

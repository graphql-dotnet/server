using GraphQL.Transport;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions;

/// <summary>
///     Manages operation execution and manages created subscriptions
/// </summary>
public interface ISubscriptionManager : IEnumerable<Subscription> //todo: add IDisposable
{
    /// <summary>
    ///     Execute operation and subscribe if subscription
    /// </summary>
    /// <param name="id"></param>
    /// <param name="payload"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task SubscribeOrExecuteAsync(string id, GraphQLRequest payload, MessageHandlingContext context);

    /// <summary>
    ///     Unsubscribe subscription
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task UnsubscribeAsync(string id);
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Manages operation execution and manages created subscriptions
    /// </summary>
    public interface ISubscriptionManager : IEnumerable<Subscription>
    {
        /// <summary>
        ///     Execute operation and subsribe if subscription
        /// </summary>
        /// <param name="id"></param>
        /// <param name="payload"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        Task SubscribeOrExecuteAsync(string id, OperationMessagePayload payload, MessageHandlingContext context);

        /// <summary>
        ///     Unsubscribe subscription
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(string id);
    }
}
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
        /// <param name="writer"></param>
        /// <returns></returns>
        Task SubscribeOrExecuteAsync(string id, OperationMessagePayload payload, IWriterPipeline writer);

        /// <summary>
        ///     Unsubscribe subscription
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(string id);
    }
}
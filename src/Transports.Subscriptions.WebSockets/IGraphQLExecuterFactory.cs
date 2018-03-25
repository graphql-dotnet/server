using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;

namespace GraphQL.Server.Transports.WebSockets
{
    /// <summary>
    ///     Factory for creating <see cref="IGraphQLExecuter"/> for given TSchema
    /// </summary>
    public interface IGraphQLExecuterFactory<TSchema> where TSchema: ISchema
    {
        /// <summary>
        ///     Create executer
        /// </summary>
        /// <typeparam name="TSchema"></typeparam>
        /// <returns></returns>
        IGraphQLExecuter Create();
    }
}
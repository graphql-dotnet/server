using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;

namespace GraphQL.Server.Transports.WebSockets
{
    public interface IGraphQLExecuterFactory
    {
        IGraphQLExecuter Create<TSchema>() where TSchema : ISchema;
    }
}
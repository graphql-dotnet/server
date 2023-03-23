namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Allows to hook up into <see cref="BaseSubscriptionServer.OnMessageReceivedAsync(OperationMessage)"/>.
/// </summary>
public interface IOperationMessageListener
{
    /// <summary>
    /// This method is called at the very beginning of <see cref="BaseSubscriptionServer.OnMessageReceivedAsync(OperationMessage)"/>
    /// </summary>
    ValueTask ListenAsync(BaseSubscriptionServer subscriptionServer, OperationMessage message);
}

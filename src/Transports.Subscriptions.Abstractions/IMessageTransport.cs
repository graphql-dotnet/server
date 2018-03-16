namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Transport defining the source of the data Reader
    ///     and target of the data Writer
    /// </summary>
    public interface IMessageTransport
    {
        /// <summary>
        ///     Pipeline from which the messages are read
        /// </summary>
        IReaderPipeline Reader { get; }

        /// <summary>
        ///     Pipeline to which the messages are written
        /// </summary>
        IWriterPipeline Writer { get; }
    }
}
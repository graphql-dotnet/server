namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// <para>
/// An asynchronous message pump, where messages intended for the client can be posted
/// on multiple threads and executed sequentially.
/// </para>
/// <para>
/// The callback functions are guaranteed to be executed in the
/// same order that <see cref="Post(T)"/> was called.
/// </para>
/// <para>
/// Callback functions may be synchronous or asynchronous.
/// Since callback functions may execute on the same thread that
/// <see cref="Post(T)"/> was called on, it is not suggested
/// to use long-running synchronous tasks within the callback
/// delegate without the use of <see cref="Task.Yield"/>.
/// </para>
/// </summary>
internal class AsyncMessagePump<T>
{
    private readonly Func<T, Task> _callback;
    private readonly Queue<ValueTask<T>> _queue = new();

    /// <summary>
    /// Initializes a new instance with the specified asynchronous callback delegate.
    /// </summary>
    public AsyncMessagePump(Func<T, Task> callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    /// <summary>
    /// Initializes a new instance with the specified synchronous callback delegate.
    /// </summary>
    public AsyncMessagePump(Action<T> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        _callback = message =>
        {
            callback(message);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Posts the specified message to the message queue.
    /// </summary>
    public void Post(T message)
        => Post(new ValueTask<T>(message));

    /// <summary>
    /// Posts the result of an asynchronous operation to the message queue.
    /// </summary>
    public void Post(ValueTask<T> messageTask)
    {
        bool attach = false;
        lock (_queue)
        {
            _queue.Enqueue(messageTask);
            attach = _queue.Count == 1;
        }

        if (attach)
        {
            _ = ProcessAllMessagesInQueueAsync();
        }
    }

    /// <summary>
    /// Processes messages from the queue in order; executes until the queue is empty.
    /// </summary>
    private async Task ProcessAllMessagesInQueueAsync()
    {
        // grab the message at the start of the queue, but don't remove it from the queue
        ValueTask<T> messageTask;
        bool moreEvents;
        lock (_queue)
        {
            // should always successfully peek from the queue here
            moreEvents = _queue.TryPeek(out messageTask);
        }
        while (moreEvents)
        {
            // process the message
            try
            {
                var message = await messageTask.ConfigureAwait(false);
                await _callback(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    await HandleErrorAsync(ex);
                }
                catch
                {
                    // if an error is unhandled, execution of this method will terminate, and
                    // no further messages in the queue will be processed, and Queue will not
                    // start another ProcessAllEventsInQueueAsync because the queue is not empty
                }
            }

            // once the message has been passed along, dequeue it
            lock (_queue)
            {
#pragma warning disable CA2012 // Use ValueTasks correctly
                _ = _queue.Dequeue();
#pragma warning restore CA2012 // Use ValueTasks correctly
                // if the queue is empty, immediately quit the loop, as any new
                // messages queued will start ProcessAllMessagesInQueueAsync
                moreEvents = _queue.TryPeek(out messageTask);
            }
        }
    }

    /// <summary>
    /// Handles exceptions that occur within the asynchronous or synchronous message delegate.
    /// By default does nothing.
    /// </summary>
    protected virtual Task HandleErrorAsync(Exception exception)
        => Task.CompletedTask;
}

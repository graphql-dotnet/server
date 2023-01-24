#if !NET6_0_OR_GREATER
namespace GraphQL.Server.Transports.AspNetCore;

internal static class TaskExtensions
{
    /// <summary>
    /// Gets a <see cref="Task{TResult}"/> that will complete when this <see cref="Task{TResult}"/> completes,
    /// when the specified timeout expires, or when the specified <see cref="CancellationToken"/> has cancellation requested.
    /// </summary>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="TimeoutException"></exception>
    public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var millisecondsTimeout = (int)timeout.TotalMilliseconds;
        if (millisecondsTimeout < -1)
            throw new ArgumentOutOfRangeException(nameof(timeout));
        if (task.IsCompleted || (millisecondsTimeout == -1 && !cancellationToken.CanBeCanceled))
            return task;
        if (millisecondsTimeout == 0)
            return Task.FromException<TResult>(new TimeoutException());
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TResult>(cancellationToken);

        return TimeoutAfter(task, millisecondsTimeout, cancellationToken);

        static async Task<TResult> TimeoutAfter(Task<TResult> task, int millisecondsDelay, CancellationToken cancellationToken)
        {
            // the CTS here ensures that the Task.Delay gets 'disposed' if the task finishes before the delay
            using var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var completedTask = await Task.WhenAny(task, Task.Delay(millisecondsDelay, timeoutCancellationTokenSource.Token)).ConfigureAwait(false);
            if (completedTask == task)
            {
                // discontinue the Task.Delay
                timeoutCancellationTokenSource.Cancel();
                return await task.ConfigureAwait(false);  // Very important in order to propagate exceptions
            }
            else
            {
                // was the cancellation token was signaled?
                cancellationToken.ThrowIfCancellationRequested();
                // or did it timeout?
                throw new TimeoutException();
            }
        }
    }
}
#endif

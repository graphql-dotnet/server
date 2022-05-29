namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// A list of subscriptions represented by <see cref="IDisposable"/> objects.
/// All members are thread-safe.
/// <br/><br/>
/// Upon signalling the cancellation token, all future calls to any method except
/// <see cref="Dispose"/> will throw an <see cref="OperationCanceledException"/>.
/// <br/><br/>
/// It is recommended to call <see cref="Dispose"/> immediately after signalling
/// the cancellation token.
/// </summary>
public sealed class SubscriptionList : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;
    private readonly Dictionary<string, IDisposable> _subscriptions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public SubscriptionList(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationToken = _cancellationTokenSource.Token;
    }

    /// <summary>
    /// Disposes of all active subscriptions.
    /// </summary>
    public void Dispose()
    {
        var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
        if (cts == null)
            return;
        cts.Cancel();
        cts.Dispose();
        List<IDisposable> subscriptionsToDispose;
        lock (_lock)
        {
            subscriptionsToDispose = _subscriptions.Values.ToList();
            _subscriptions.Clear();
        }
        foreach (var disposer in subscriptionsToDispose)
        {
            disposer.Dispose();
        }
    }

    /// <summary>
    /// Adds a subscription to the internal list if none is already in the list with the same id.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="OperationCanceledException"/>
    public bool TryAdd(string id, IDisposable subscription)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (subscription == null)
            throw new ArgumentNullException(nameof(subscription));

        lock (_lock)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _subscriptions.TryAdd(id, subscription);
        }
    }

    /// <summary>
    /// Adds a subscription to the internal list, overwriting an existing registration if any.
    /// When overwriting an existing registration, the old registration is disposed.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="OperationCanceledException"/>
    public IDisposable this[string id]
    {
        set
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            IDisposable? oldDisposable = null;
            try
            {
                lock (_lock)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _subscriptions.TryGetValue(id, out oldDisposable);
                    _subscriptions[id] = value;
                }
            }
            finally
            {
                oldDisposable?.Dispose();
            }
        }
    }

    /// <summary>
    /// Validates that the specified subscription is still active.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="OperationCanceledException"/>
    public bool Contains(string id, IDisposable subscription)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (subscription == null)
            throw new ArgumentNullException(nameof(subscription));

        lock (_lock)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _subscriptions.TryGetValue(id, out var value) && value == subscription;
        }
    }

    /// <summary>
    /// Validates that the specified subscription is still active.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="OperationCanceledException"/>
    public bool Contains(string id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        lock (_lock)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            return _subscriptions.ContainsKey(id);
        }
    }

    /// <summary>
    /// Exchanges the specified subscription with another implementation for the specified id.
    /// Disposes of the old subscription if exchanged.
    /// Returns <see langword="false"/> if no subscription can be found.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="OperationCanceledException"/>
    public bool CompareExchange(string id, IDisposable oldSubscription, IDisposable newSubscription)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (oldSubscription == null)
            throw new ArgumentNullException(nameof(oldSubscription));
        if (newSubscription == null)
            throw new ArgumentNullException(nameof(newSubscription));

        bool dispose = false;
        try
        {
            lock (_lock)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (!_subscriptions.TryGetValue(id, out var value) || value != oldSubscription)
                    return false;
                _subscriptions[id] = newSubscription;
                dispose = true;
                return true;
            }
        }
        finally
        {
            if (dispose)
                oldSubscription.Dispose();
        }
    }

    /// <summary>
    /// Removes the specified subscription and disposes of it.
    /// Returns <see langword="false"/> if no subscription can be found.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public bool TryRemove(string id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        IDisposable? subscription = null;
        try
        {
            lock (_lock)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (_subscriptions.TryGetValue(id, out subscription))
                {
                    _subscriptions.Remove(id);
                    return true;
                }
                return false;
            }
        }
        finally
        {
            subscription?.Dispose();
        }
    }

    /// <summary>
    /// Removes the specified subscription and disposes of it.
    /// Returns <see langword="false"/> if no subscription can be found.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    public bool TryRemove(string id, IDisposable oldSubscription)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (oldSubscription == null)
            throw new ArgumentNullException(nameof(oldSubscription));

        bool dispose = false;
        try
        {
            lock (_lock)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (!_subscriptions.TryGetValue(id, out var value) || value != oldSubscription)
                    return false;
                _subscriptions.Remove(id);
                dispose = true;
                return true;
            }
        }
        finally
        {
            if (dispose)
                oldSubscription.Dispose();
        }
    }
}

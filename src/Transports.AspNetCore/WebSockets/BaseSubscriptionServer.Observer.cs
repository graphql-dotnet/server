namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

public abstract partial class BaseSubscriptionServer
{
    /// <summary>
    /// Handles messages from the event source.
    /// </summary>
    private class Observer : IObserver<ExecutionResult>
    {
        private readonly BaseSubscriptionServer _handler;
        private readonly string _id;
        private readonly bool _closeAfterOnError;
        private readonly bool _closeAfterAnyError;
        private int _done;

        public Observer(BaseSubscriptionServer handler, string id, bool closeAfterOnError, bool closeAfterAnyError)
        {
            _handler = handler;
            _id = id;
            _closeAfterOnError = closeAfterOnError;
            _closeAfterAnyError = closeAfterAnyError;
        }

        public void OnCompleted()
        {
            if (Interlocked.Exchange(ref _done, 1) == 1)
                return;
            try
            {
                _ = _handler.SendCompletedAsync(_id);
            }
            catch { }
        }

        public async void OnError(Exception error)
        {
            if (_closeAfterOnError)
            {
                if (Interlocked.Exchange(ref _done, 1) == 1)
                    return;
            }
            else
            {
                if (Interlocked.CompareExchange(ref _done, 0, 0) == 1)
                    return;
            }
            try
            {
                if (error != null)
                {
                    var executionError = error is ExecutionError ee ? ee : await _handler.HandleErrorFromSourceAsync(error);
                    if (executionError != null)
                    {
                        var result = new ExecutionResult
                        {
                            Errors = new ExecutionErrors { executionError },
                        };
                        await _handler.SendDataAsync(_id, result);
                    }
                }
            }
            catch { }
            try
            {
                if (_closeAfterOnError)
                    await _handler.SendCompletedAsync(_id);
            }
            catch { }
        }

        public async void OnNext(ExecutionResult value)
        {
            if (Interlocked.CompareExchange(ref _done, 0, 0) == 1)
                return;
            if (value == null)
                return;
            try
            {
                await _handler.SendDataAsync(_id, value);
                if (_closeAfterAnyError && value.Errors != null && value.Errors.Count > 0)
                {
                    await _handler.SendCompletedAsync(_id);
                }
            }
            catch { }
        }
    }
}

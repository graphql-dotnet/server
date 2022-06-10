namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

public abstract partial class BaseSubscriptionServer
{
    /// <summary>
    /// Handles messages from the event source.
    /// </summary>
    private class Observer : IObserver<ExecutionResult>
    {
        private readonly BaseSubscriptionServer _server;
        private readonly string _id;
        private readonly bool _closeAfterOnError;
        private readonly bool _closeAfterAnyError;
        private int _done;

        public Observer(BaseSubscriptionServer server, string id, bool closeAfterOnError, bool closeAfterAnyError)
        {
            _server = server;
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
                _ = _server.SendCompletedAsync(_id);
            }
            catch { }
        }

        public async void OnError(Exception error)
        {
            if (Thread.VolatileRead(ref _done) == 1)
                return;
            if (_closeAfterOnError && Interlocked.Exchange(ref _done, 1) == 1)
                return;
            try
            {
                // although error should never be null, if the event source does call OnError(null!),
                // skip sending an error packet/message (allowed by spec)
                if (error != null)
                {
                    var executionError = error is ExecutionError ee ? ee : await _server.HandleErrorFromSourceAsync(error);
                    if (executionError != null)
                    {
                        var result = new ExecutionResult
                        {
                            Errors = new ExecutionErrors { executionError },
                        };
                        await _server.SendDataAsync(_id, result);
                    }
                }
            }
            catch { }
            try
            {
                if (_closeAfterOnError)
                    await _server.SendCompletedAsync(_id);
            }
            catch { }
        }

        public async void OnNext(ExecutionResult value)
        {
            if (value == null || Thread.VolatileRead(ref _done) == 1)
                return;
            try
            {
                await _server.SendDataAsync(_id, value);
                if (_closeAfterAnyError && value.Errors?.Count > 0)
                {
                    await _server.SendCompletedAsync(_id);
                }
            }
            catch { }
        }
    }
}

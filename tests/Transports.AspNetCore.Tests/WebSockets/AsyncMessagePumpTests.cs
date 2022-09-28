using System.Globalization;

namespace Tests.WebSockets;

public class AsyncMessagePumpTests
{
    [Fact]
    public void NullCallbackThrows()
    {
        Assert.ThrowsAny<ArgumentNullException>(() => new AsyncMessagePump<string>(null!));
    }

    [Fact]
    public void CallsSynchronously()
    {
        var sb = new StringBuilder();
        var pump = new AsyncMessagePump<string>(t =>
        {
            Thread.Sleep(int.Parse(t, CultureInfo.InvariantCulture));
            sb.Append(t + " ");
            return Task.CompletedTask;
        });
        pump.Post("100");
        pump.Post("1");
        Assert.Equal("100 1 ", sb.ToString());
    }

    [Fact]
    public async Task CallsAsynchronously()
    {
        var reset = new TaskCompletionSource<bool>();
        var sb = new StringBuilder();
        var pump = new AsyncMessagePump<string>(async t =>
        {
            await Task.Delay(int.Parse(t, CultureInfo.InvariantCulture));
            lock (sb)
                sb.Append(t + " ");
            if (t == "1")
                reset.SetResult(true);
        });
        pump.Post("100");
        pump.Post("1");
        await reset.Task;
        lock (sb)
            Assert.Equal("100 1 ", sb.ToString());
    }

    [Fact]
    public async Task CallsAsynchronouslyTasks()
    {
        var reset = new TaskCompletionSource<bool>();
        var sb = new StringBuilder();
        var pump = new AsyncMessagePump<string>(async t =>
        {
            await Task.Delay(int.Parse(t, CultureInfo.InvariantCulture));
            lock (sb)
                sb.Append(t + " ");
            if (t == "1")
                reset.SetResult(true);
        });
        Func<ValueTask<string>> func = async () =>
        {
            await Task.Delay(100);
            return "100";
        };
        pump.Post(func());
        pump.Post("1");
        await reset.Task;
        lock (sb)
            Assert.Equal("100 1 ", sb.ToString());
    }

    [Fact]
    public void HandlesErrorsGracefully()
    {
        var sb = new StringBuilder();
        var pump = new AsyncMessagePump<string>(t =>
        {
            int.Parse(t, CultureInfo.InvariantCulture);
            sb.Append(t + " ");
        });
        pump.Post("1");
        pump.Post("test");
        pump.Post("2");
        Assert.Equal("1 2 ", sb.ToString());
    }

    [Fact]
    public void HandlesErrorsGracefullyDerived()
    {
        var sb = new StringBuilder();
        var pump = new DerivedAsyncMessagePump(t =>
        {
            int.Parse(t, CultureInfo.InvariantCulture);
            sb.Append(t + " ");
        }, sb);
        pump.Post("1");
        pump.Post("test");
        pump.Post("2");
        Assert.Equal("1 FormatException 2 ", sb.ToString());
    }

    private class DerivedAsyncMessagePump : AsyncMessagePump<string>
    {
        private readonly StringBuilder _sb;
        public DerivedAsyncMessagePump(Action<string> func, StringBuilder sb) : base(func)
        {
            _sb = sb;
        }

        protected override Task HandleErrorAsync(Exception exception)
        {
            _sb.Append(exception.GetType().Name + " ");
            throw new Exception("testing");
        }
    }
}

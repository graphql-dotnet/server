namespace GraphQL.Server.Transports.AspNetCore;

internal class NullHostApplicationLifetime : IHostApplicationLifetime
{
    private NullHostApplicationLifetime()
    {
    }

    public static NullHostApplicationLifetime Instance { get; } = new();

    public CancellationToken ApplicationStarted => default;

    public CancellationToken ApplicationStopped => default;

    public CancellationToken ApplicationStopping => default;

    public void StopApplication() { }
}

namespace GraphQL.Server.Transports.AspNetCore;

#if NETSTANDARD2_0 || NETCOREAPP2_1

/// <summary>
/// Provides a signal when the application is shutting down.
/// </summary>
public interface IHostApplicationLifetime
{
    /// <inheritdoc cref="IHostApplicationLifetime"/>
    CancellationToken ApplicationStopping { get; }
}

/// <inheritdoc cref="IHostApplicationLifetime"/>
public class HostApplicationLifetime : IHostApplicationLifetime, IHostedService
{
    private readonly CancellationTokenSource _cts = new();

    /// <inheritdoc/>
    public CancellationToken ApplicationStopping => _cts.Token;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IHostApplicationLifetime"/> within the dependency injection framework.
    /// </summary>
    public static void AddHostApplicationLifetime(this IServiceCollection services)
    {
        services.AddSingleton<IHostApplicationLifetime, HostApplicationLifetime>();
        services.AddSingleton<IHostedService>(provider => (HostApplicationLifetime)provider.GetRequiredService<IHostApplicationLifetime>());
    }
}

#endif

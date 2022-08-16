#if NETSTANDARD2_0 || NETCOREAPP2_1

namespace GraphQL.Server.Transports.AspNetCore;

internal interface IAsyncDisposable
{
    ValueTask DisposeAsync();
}

#endif

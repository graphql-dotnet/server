using GraphQL.Execution;
using Microsoft.Extensions.Options;

namespace GraphQL.Server
{
    /// <summary>
    /// The default <see cref="ErrorInfoProvider"/> for ASP.NET Core applications providing integration
    /// with Microsoft.Extensions.Options so the caller may use services.Configure{ErrorInfoProviderOptions}(...)
    /// </summary>
    public class DefaultErrorInfoProvider : ErrorInfoProvider
    {
        public DefaultErrorInfoProvider(IOptions<ErrorInfoProviderOptions> options)
            : base(options.Value) { }
    }
}

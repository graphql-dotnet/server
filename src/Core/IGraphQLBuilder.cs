using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    /// <summary>
    /// GraphQL builder used for GraphQL specific extension methods as 'this' argument.
    /// </summary>
    public interface IGraphQLBuilder
    {
        /// <summary>
        /// Underlying <see cref="IServiceCollection"/> collection used (filled) by extension methods.
        /// </summary>
        IServiceCollection Services { get; }
    }
}

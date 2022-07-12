#if !NETSTANDARD2_0

using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="IEndpointRouteBuilder"/> to add <see cref="PlaygroundMiddleware"/> in the HTTP request pipeline.
/// </summary>
public static class PlaygroundEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Add the Playground middleware to the HTTP request pipeline
    /// </summary>
    /// <param name="endpoints">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <param name="options">Options to customize <see cref="PlaygroundMiddleware"/>. If not set, then the default values will be used.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
    public static PlaygroundEndpointConventionBuilder MapGraphQLPlayground(this IEndpointRouteBuilder endpoints, string pattern = "ui/playground", PlaygroundOptions? options = null)
    {
        if (endpoints == null)
            throw new ArgumentNullException(nameof(endpoints));

        var requestDelegate = endpoints.CreateApplicationBuilder().UseMiddleware<PlaygroundMiddleware>(options ?? new PlaygroundOptions()).Build();
        return new PlaygroundEndpointConventionBuilder(endpoints.MapGet(pattern, requestDelegate).WithDisplayName("GraphQL Playground"));
    }
}

/// <summary>
/// Builds conventions that will be used for customization of Microsoft.AspNetCore.Builder.EndpointBuilder instances.
/// Special convention builder that allows you to write specific extension methods for ASP.NET Core routing subsystem.
/// </summary>
public class PlaygroundEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IEndpointConventionBuilder _builder;

    internal PlaygroundEndpointConventionBuilder(IEndpointConventionBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention) => _builder.Add(convention);
}

#endif

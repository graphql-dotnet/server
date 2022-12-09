#pragma warning disable CA1716 // Identifiers should not match keywords

namespace GraphQL.Server.Transports.AspNetCore.AzureFunctions;

/// <summary>
/// Defines middleware for executing GraphQL documents in Azure Functions.
/// </summary>
public interface IAzureGraphQLMiddleware
{
    /// <inheritdoc cref="IMiddleware.InvokeAsync(HttpContext, RequestDelegate)"/>
    Task InvokeAsync(HttpRequest request, RequestDelegate next);
}

/// <inheritdoc cref="IAzureGraphQLMiddleware"/>
public interface IAzureGraphQLMiddleware<TSchema> : IAzureGraphQLMiddleware
    where TSchema : ISchema
{
}


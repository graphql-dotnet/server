namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Authorization parameters.
/// This struct is used to group all necessary parameters together and perform arbitrary
/// actions based on provided authentication properties/attributes/etc.
/// It is not intended to be called from user code.
/// </summary>
public readonly struct AuthorizationParameters<TState>
{
    /// <summary>
    /// Initializes an instance with a specified <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
    /// and parameters copied from the specified instance of <see cref="GraphQLHttpMiddlewareOptions"/>.
    /// </summary>
    public AuthorizationParameters(
        HttpContext httpContext,
        IAuthorizationOptions authorizationOptions,
        Func<TState, Task>? onNotAuthenticated,
        Func<TState, Task>? onNotAuthorizedRole,
        Func<TState, AuthorizationResult, Task>? onNotAuthorizedPolicy)
    {
        HttpContext = httpContext;
        AuthorizationRequired = authorizationOptions.AuthorizationRequired;
        AuthorizedRoles = authorizationOptions.AuthorizedRoles;
        AuthorizedPolicy = authorizationOptions.AuthorizedPolicy;
        OnNotAuthenticated = onNotAuthenticated;
        OnNotAuthorizedRole = onNotAuthorizedRole;
        OnNotAuthorizedPolicy = onNotAuthorizedPolicy;
    }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.HttpContext"/> for the request.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <inheritdoc cref="GraphQLHttpMiddlewareOptions.AuthorizationRequired"/>
    public bool AuthorizationRequired { get; }

    /// <inheritdoc cref="GraphQLHttpMiddlewareOptions.AuthorizedRoles"/>
    public IEnumerable<string>? AuthorizedRoles { get; }

    /// <inheritdoc cref="GraphQLHttpMiddlewareOptions.AuthorizedPolicy"/>
    public string? AuthorizedPolicy { get; }

    /// <summary>
    /// A delegate which executes if <see cref="AuthorizationRequired"/> is set
    /// but <see cref="IIdentity.IsAuthenticated"/> returns <see langword="false"/>.
    /// </summary>
    public Func<TState, Task>? OnNotAuthenticated { get; }

    /// <summary>
    /// A delegate which executes if <see cref="AuthorizedRoles"/> is set but
    /// <see cref="ClaimsPrincipal.IsInRole(string)"/> returns <see langword="false"/>
    /// for all roles.
    /// </summary>
    public Func<TState, Task>? OnNotAuthorizedRole { get; }

    /// <summary>
    /// A delegate which executes if <see cref="AuthorizedPolicy"/> is set but
    /// <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, string)"/>
    /// returns an unsuccessful <see cref="AuthorizationResult"/> for the specified policy.
    /// </summary>
    public Func<TState, AuthorizationResult, Task>? OnNotAuthorizedPolicy { get; }
}

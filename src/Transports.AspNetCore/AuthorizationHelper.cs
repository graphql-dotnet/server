namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Helper methods for performing connection authorization.
/// </summary>
public static class AuthorizationHelper
{
    /// <summary>
    /// Performs connection authorization according to the options set within
    /// <see cref="AuthorizationParameters{TState}"/>.  Returns <see langword="true"/>
    /// if authorization was successful or not required.
    /// </summary>
    public static async ValueTask<bool> AuthorizeAsync<TState>(AuthorizationParameters<TState> options, TState state)
    {
        var anyRolesRequired = options.AuthorizedRoles?.Any() ?? false;

        if (options.AuthorizationRequired || anyRolesRequired || options.AuthorizedPolicy != null)
        {
            if (!((options.HttpContext.User ?? NoUser()).Identity ?? NoIdentity()).IsAuthenticated)
            {
                if (options.OnNotAuthenticated != null)
                    await options.OnNotAuthenticated(state);
                return false;
            }
        }

        if (anyRolesRequired)
        {
            var user = options.HttpContext.User ?? NoUser();
            foreach (var role in options.AuthorizedRoles!)
            {
                if (user.IsInRole(role))
                    goto PassRoleCheck;
            }
            if (options.OnNotAuthorizedRole != null)
                await options.OnNotAuthorizedRole(state);
            return false;
        }
    PassRoleCheck:

        if (options.AuthorizedPolicy != null)
        {
            var authorizationService = options.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var authResult = await authorizationService.AuthorizeAsync(options.HttpContext.User ?? NoUser(), null, options.AuthorizedPolicy);
            if (!authResult.Succeeded)
            {
                if (options.OnNotAuthorizedPolicy != null)
                    await options.OnNotAuthorizedPolicy(state, authResult);
                return false;
            }
        }

        return true;
    }

    private static IIdentity NoIdentity()
        => throw new InvalidOperationException($"IIdentity could not be retrieved from HttpContext.User.Identity.");

    private static ClaimsPrincipal NoUser()
        => throw new InvalidOperationException("ClaimsPrincipal could not be retrieved from HttpContext.User.");
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Core.Authorization
{
    public static class HttpAuthorizationHelper
    {
        /// <summary>
        /// Authorize current user based on provided options
        /// </summary>
        /// <param name="context"></param>
        /// <param name="policyName"></param>
        /// <returns>
        /// True if the user was authorized and the request should proceed.
        /// False if the request should be ended immediately--a challenge or forbidden response has been issued.
        /// </returns>
        public static async Task<bool> AuthorizeAsync(HttpContext context, string policyName)
        {
            if (policyName == null)
            {
                return true;
            }

            // Get policy to apply
            var policyProvider = context.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
            var policy = await policyProvider.GetPolicyAsync(policyName);

            if (policy == null)
            {
                return true;
            }

            var policyEvaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();

            // Make sure user is authenticated
            var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, context);

            // Check authorization policy
            var authorizeResult = await policyEvaluator.AuthorizeAsync(policy, authenticateResult, context, null);

            if (authorizeResult.Succeeded)
            {
                return true;
            }
            else if (authorizeResult.Challenged)
            {
                // Issue authorization challenge
                if (policy.AuthenticationSchemes?.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ChallengeAsync(scheme);
                    }
                }
                else
                {
                    await context.ChallengeAsync();
                }

                return false;
            }
            else if (authorizeResult.Forbidden)
            {
                if (policy.AuthenticationSchemes.Count > 0)
                {
                    foreach (var scheme in policy.AuthenticationSchemes)
                    {
                        await context.ForbidAsync(scheme);
                    }
                }
                else
                {
                    await context.ForbidAsync();
                }
                return false;
            }

            throw new InvalidOperationException("Unexpected PolicyAuthorizationResult");
        }
    }
}

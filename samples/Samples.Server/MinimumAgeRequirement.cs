using Microsoft.AspNetCore.Authorization;

namespace GraphQL.Samples.Server
{
    /// <summary>
    /// A <see cref="IAuthorizationRequirement"/> enforcing a minimum user age.
    /// (sample taken from https://docs.microsoft.com/en-us/aspnet/core/security/authorization/policies)
    /// </summary>
    public class MinimumAgeRequirement : IAuthorizationRequirement
    {
        public int MinimumAge { get; }

        public MinimumAgeRequirement(int minimumAge)
        {
            MinimumAge = minimumAge;
        }
    }
}

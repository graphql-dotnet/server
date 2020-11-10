using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;

namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// This policy resolves 'Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException: The request matched multiple endpoints'
    /// when both GraphQL HTTP and GraphQL WebSockets middlewares are mapped to the same endpoint (by default 'graphql').
    /// </summary>
    internal sealed class GraphQLDefaultEndpointSelectorPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        public override int Order => int.MaxValue;

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            for (int i = 0; i < endpoints.Count; ++i)
            {
                if (endpoints[i].DisplayName == "GraphQL" || endpoints[i].DisplayName == "GraphQL WebSockets")
                    return true;
            }

            return false;
        }

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            if (candidates.Count < 2)
                return Task.CompletedTask;

            for (int i = 0; i < candidates.Count; ++i)
            {
                if (!candidates.IsValidCandidate(i))
                    continue;

                ref var state = ref candidates[i];

                if (state.Endpoint.DisplayName == "GraphQL" && httpContext.WebSockets.IsWebSocketRequest)
                {
                    candidates.SetValidity(i, false);
                }

                if (state.Endpoint.DisplayName == "GraphQL WebSockets" && !httpContext.WebSockets.IsWebSocketRequest)
                {
                    candidates.SetValidity(i, false);
                }
            }

            return Task.CompletedTask;
        }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Add HTTP authorization services
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddHttpAuthorization(this IGraphQLBuilder builder)
        {
            builder.Services.AddAuthorization();
            builder.Services.AddAuthorizationPolicyEvaluator();

            return builder;
        }
    }
}

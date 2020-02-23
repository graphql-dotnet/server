using GraphQL.Builders;
using GraphQL.Types;
using System.Collections.Generic;

namespace GraphQL.Server.Authorization.AspNetCore
{
    public static class AuthorizationMetadataExtensions
    {
        public const string POLICY_KEY = "Authorization__Policies";

        public static bool RequiresAuthorization(this IProvideMetadata type)
        {
            var policies = GetPolicies(type);
            return policies != null && policies.Count > 0;
        }

        public static void AuthorizeWith(this IProvideMetadata type, string policy)
        {
            var list = GetPolicies(type) ?? new List<string>();
            if (!list.Contains(policy))
            {
                list.Add(policy);
            }
            type.Metadata[POLICY_KEY] = list;
        }

        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWith<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string policy)
        {
            builder.FieldType.AuthorizeWith(policy);
            return builder;
        }

        public static ConnectionBuilder<TSourceType> AuthorizeWith<TSourceType>(
            this ConnectionBuilder<TSourceType> builder, string policy)
        {
            builder.FieldType.AuthorizeWith(policy);
            return builder;
        }

        public static List<string> GetPolicies(this IProvideMetadata type) => type.GetMetadata<List<string>>(POLICY_KEY);
    }
}

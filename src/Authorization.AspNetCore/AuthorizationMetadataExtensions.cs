using System.Collections.Generic;
using System.Linq;
using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.Server.Authorization.AspNetCore
{
    public static class AuthorizationMetadataExtensions
    {
        public const string PolicyKey = "Authorization__Policies";

        public static bool RequiresAuthorization(this IProvideMetadata type) => GetPolicies(type).Any();

        public static void AuthorizeWith(this IProvideMetadata type, string policy)
        {
            var list = GetPolicies(type);
            list.Fill(policy);
            type.Metadata[PolicyKey] = list;
        }

        public static FieldBuilder<TSourceType, TReturnType> AuthorizeWith<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder, string policy)
        {
            builder.FieldType.AuthorizeWith(policy);
            return builder;
        }

        public static List<string> GetPolicies(this IProvideMetadata type) =>
            type.GetMetadata<List<string>>(PolicyKey, null);
    }
}
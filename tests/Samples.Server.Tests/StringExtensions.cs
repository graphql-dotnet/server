using GraphQL;

namespace Samples.Server.Tests;

internal static class StringExtensions
{
    public static Inputs ToInputs(this string value)
        => new GraphQL.SystemTextJson.GraphQLSerializer().Deserialize<Inputs>(value);
}

using GraphQL.Types;

namespace GraphQL.Server.Samples.NativeAot.GraphTypes;

public class QueryType : ObjectGraphType
{
    public QueryType()
    {
        Field<StringGraphType>("hello")
            .Resolve(context => "world");
    }
}

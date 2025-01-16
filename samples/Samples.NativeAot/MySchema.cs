using GraphQL.Server.Samples.NativeAot.GraphTypes;
using GraphQL.Types;

namespace GraphQL.Server.Samples.NativeAot;

public class MySchema : Schema
{
    public MySchema(IServiceProvider services, QueryType queryType)
        : base(services)
    {
        Query = queryType;
    }
}

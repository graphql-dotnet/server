using GraphQL;
using GraphQLSchema = global::GraphQL.Types.Schema;
using Demo.Azure.Functions.GraphQL.Schema.Queries;

namespace Demo.Azure.Functions.GraphQL.Schema
{
    public class StarWarsSchema: GraphQLSchema
    {
        public StarWarsSchema(IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            Query = dependencyResolver.Resolve<PlanetQuery>();
        }
    }
}

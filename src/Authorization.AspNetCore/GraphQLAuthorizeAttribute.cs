using GraphQL.Utilities;

namespace GraphQL.Server.Authorization.AspNetCore
{
    public class GraphQLAuthorizeAttribute : GraphQLAttribute
    {
        public string Policy { get; set; }

        public override void Modify(TypeConfig type) => type.AuthorizeWith(Policy);

        public override void Modify(FieldConfig field) => field.AuthorizeWith(Policy);
    }
}

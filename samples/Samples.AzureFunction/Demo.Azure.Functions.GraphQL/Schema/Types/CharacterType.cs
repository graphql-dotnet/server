using GraphQL.Types;
using Demo.Azure.Functions.GraphQL.Documents;

namespace Demo.Azure.Functions.GraphQL.Schema.Types
{
    internal class CharacterType : ObjectGraphType<Character>
    {
        public CharacterType()
        {
            Field(t => t.CharacterId);
            Field(t => t.Name);
            Field(t => t.BirthYear);
        }
    }
}

using GraphQL.Types;

namespace GraphQL.Samples.Schemas.Chat
{
    public class MessageFromType : ObjectGraphType<MessageFrom>
    {
        public MessageFromType()
        {
            Field(o => o.Id);
            Field(o => o.DisplayName);
        }
    }
}

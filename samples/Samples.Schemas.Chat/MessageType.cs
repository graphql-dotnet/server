using GraphQL.Types;

namespace GraphQL.Samples.Schemas.Chat
{
    public class MessageType : ObjectGraphType<Message>
    {
        public MessageType()
        {
            Field(o => o.Content);
            Field(o => o.SentAt);
            Field(o => o.Sub);
            Field(o => o.From, false, typeof(MessageFromType)).Resolve(ResolveFrom);
        }

        private MessageFrom ResolveFrom(ResolveFieldContext<Message> context)
        {
            var message = context.Source;
            return message.From;
        }
    }
}

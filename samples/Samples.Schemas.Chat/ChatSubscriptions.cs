using System;
using System.Reactive.Linq;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;

namespace GraphQL.Samples.Schemas.Chat
{
    public class ChatSubscriptions : ObjectGraphType<object>
    {
        private readonly IChat _chat;

        public ChatSubscriptions(IChat chat)
        {
            _chat = chat;
            AddField(new EventStreamFieldType
            {
                Name = "messageAdded",
                Type = typeof(MessageType),
                Resolver = new FuncFieldResolver<Message>(ResolveMessage),
                Subscriber = new EventStreamResolver<Message>(Subscribe)
            });

            AddField(new EventStreamFieldType
            {
                Name = "messageAddedByUser",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                ),
                Type = typeof(MessageType),
                Resolver = new FuncFieldResolver<Message>(ResolveMessage),
                Subscriber = new EventStreamResolver<Message>(SubscribeById)
            });
        }

        private IObservable<Message> SubscribeById(ResolveEventStreamContext context)
        {
            var id = context.GetArgument<string>("id");

            var messages =  _chat.Messages();

            return messages.Where(message => message.From.Id == id);
        }

        private Message ResolveMessage(ResolveFieldContext context)
        {
            var message = context.Source as Message;

            return message;
        }

        private IObservable<Message> Subscribe(ResolveEventStreamContext context)
        {
            return _chat.Messages();
        }
    }
}

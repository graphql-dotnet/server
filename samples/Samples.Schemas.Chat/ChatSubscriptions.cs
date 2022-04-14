using System.Reactive.Linq;
using System.Security.Claims;
using GraphQL.Resolvers;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;

namespace GraphQL.Samples.Schemas.Chat;

public class ChatSubscriptions : ObjectGraphType<object>
{
    private readonly IChat _chat;

    public ChatSubscriptions(IChat chat)
    {
        _chat = chat;
        AddField(new FieldType
        {
            Name = "messageAdded",
            Type = typeof(MessageType),
            Resolver = new FuncFieldResolver<Message>(ResolveMessage),
            StreamResolver = new SourceStreamResolver<Message>(Subscribe)
        });

        AddField(new FieldType
        {
            Name = "messageAddedByUser",
            Arguments = new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
            ),
            Type = typeof(MessageType),
            Resolver = new FuncFieldResolver<Message>(ResolveMessage),
            StreamResolver = new SourceStreamResolver<Message>(SubscribeById)
        });
    }

    private IObservable<Message> SubscribeById(IResolveFieldContext context)
    {
        var messageContext = (MessageHandlingContext)context.UserContext;
        var user = messageContext.Get<ClaimsPrincipal>("user");

        string sub = "Anonymous";
        if (user != null)
            sub = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        var messages = _chat.Messages(sub);

        string id = context.GetArgument<string>("id");
        return messages.Where(message => message.From.Id == id);
    }

    private Message ResolveMessage(IResolveFieldContext context)
    {
        var message = context.Source as Message;

        return message;
    }

    private IObservable<Message> Subscribe(IResolveFieldContext context)
    {
        var messageContext = (MessageHandlingContext)context.UserContext;
        var user = messageContext.Get<ClaimsPrincipal>("user");

        string sub = "Anonymous";
        if (user != null)
            sub = user.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        return _chat.Messages(sub);
    }
}

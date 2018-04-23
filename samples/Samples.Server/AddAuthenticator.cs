using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Server.Transports.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MessageType = GraphQL.Server.Transports.Subscriptions.Abstractions.MessageType;

namespace GraphQL.Samples.Server
{
    public class AddAuthenticator : IPostConfigureOptions<ExecutionOptions<ChatSchema>>
    {
        private readonly IOperationMessageListener _authenticator;

        public AddAuthenticator(ITokenListener tokenListener)
        {
            _authenticator = tokenListener;
        }

        public void PostConfigure(string name, ExecutionOptions<ChatSchema> options)
        {
            options.MessageListeners.Insert(0, _authenticator);
        }
    }

    public interface ITokenListener : IOperationMessageListener
    {
    }

    public class TokenListener : ITokenListener
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenListener(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Task BeforeHandleAsync(MessageHandlingContext context)
        {
            if (context.Message.Type == MessageType.GQL_CONNECTION_INIT)
            {
                var token = context.Message.Payload.Value<string>("token");

                if (!string.IsNullOrEmpty(token))
                {
                    _httpContextAccessor.HttpContext
                        .User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(JwtRegisteredClaimNames.Sub, token)
                            }
                        ));
                }
            }

            var user = _httpContextAccessor.HttpContext.User;
            context.Properties["user"] = user;
            return Task.FromResult(true);
        }

        public Task HandleAsync(MessageHandlingContext context)
        {
            return Task.CompletedTask;
        }

        public Task AfterHandleAsync(MessageHandlingContext context)
        {
            return Task.CompletedTask;
        }
    }
}
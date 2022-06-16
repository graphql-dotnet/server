using Microsoft.AspNetCore.Http;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2Client = OAuth2.Client;

namespace GraphQL.Server.Ui.SmartPlayground.Smart
{
    public class SmartClient : OAuth2Client.OAuth2Client, ISmartClient
    {
        private readonly SmartConfiguration _smartConfiguration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmartClient(IHttpContextAccessor httpContextAccessor, IRequestFactory factory, SmartConfiguration smartConfiguration)
            : base(factory, new ClientConfiguration
            {
                ClientId = smartConfiguration.ClientId,
                RedirectUri = smartConfiguration.RedirectUri,
                Scope = smartConfiguration.Scope
            })
        {
            _httpContextAccessor = httpContextAccessor;
            _smartConfiguration = smartConfiguration;
        }

        public async Task Launch()
        {
            var loginLinkUri = await GetLoginLinkUriAsync();
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("missing httpContext in Launch");
            }

            httpContext.Response.Redirect(loginLinkUri);
        }

        public async Task<string> Redirect(string code)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("missing httpContext in Redirect");
            }

            var token = await GetTokenAsync(new System.Collections.Specialized.NameValueCollection { { "code", code } });
            if (!string.IsNullOrEmpty(token))
            {
                httpContext.Response.Cookies.Append("token", token);
            }

            // Clear all parameters in URL
            httpContext.Response.Redirect(_smartConfiguration.RedirectUri);

            return token;
        }

        public override string Name => "SMART OAuth2 Client";

        protected override OAuth2Client.Endpoint AccessCodeServiceEndpoint => new OAuth2Client.Endpoint { BaseUri = _smartConfiguration.Authorization.ToString() };

        protected override OAuth2Client.Endpoint AccessTokenServiceEndpoint => new OAuth2Client.Endpoint { BaseUri = _smartConfiguration.Token.ToString() };

        protected override OAuth2Client.Endpoint UserInfoServiceEndpoint => throw new NotImplementedException();

        protected override UserInfo ParseUserInfo(string content)
        {
            throw new NotImplementedException();
        }
    }
}

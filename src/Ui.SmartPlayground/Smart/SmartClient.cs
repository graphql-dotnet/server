using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAuth2.Configuration;
using OAuth2.Infrastructure;
using OAuth2.Models;
using OAuth2Client = OAuth2.Client;

namespace GraphQL.Server.Ui.SmartPlayground.Smart
{
    public class SmartClient : OAuth2Client.OAuth2Client, ISmartClient
    {

        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly OAuth2Settings _settings;

        public SmartClient(ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, IRequestFactory factory, IOptions<OAuth2Settings> settings)
            : base(factory, new ClientConfiguration
            {
                ClientId = Constants.SmartClientId,
                RedirectUri = settings.Value.SafeBaseUrl + Constants.SmartPlaygroundPath,
                Scope = Constants.SmartScope
            })
        {
            _logger = loggerFactory.CreateLogger<SmartClient>();
            _httpContextAccessor = httpContextAccessor;
            _settings = settings?.Value ?? throw new InvalidDataException("Missing OAuth2 section in configuration!");
        }

        public async Task Launch()
        {

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                var exception = new InvalidOperationException("missing httpContext in Launch");
                _logger.LogError(exception.Message, exception);
                throw exception;
            }

            var loginLinkUri = await GetLoginLinkUriAsync();

            _logger.LogInformation($"Launch - Redirecting to: {loginLinkUri}");

            httpContext.Response.Redirect(loginLinkUri);
        }

        public async Task<string> Redirect(string code)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                var exception = new InvalidOperationException("missing httpContext in Redirect");
                _logger.LogError(exception.Message, exception);
                throw exception;
            }

            var token = await GetTokenAsync(new System.Collections.Specialized.NameValueCollection { { "code", code } });
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Redirect - adding token to header");
                httpContext.Response.Cookies.Append("token", token);
            }

            // Clear all parameters in URL

            var cleanUrl = _settings.SafeBaseUrl + Constants.SmartPlaygroundPath;
            _logger.LogInformation($"Redirect - Redirecting to: {cleanUrl}");
            httpContext.Response.Redirect(cleanUrl);

            return token;
        }

        public override string Name => "SMART OAuth2 Client";


        protected override OAuth2Client.Endpoint AccessCodeServiceEndpoint => new OAuth2Client.Endpoint { BaseUri = _settings.SafeAuthorizeUrl.ToString() };

        protected override OAuth2Client.Endpoint AccessTokenServiceEndpoint => new OAuth2Client.Endpoint { BaseUri = _settings.SafeTokenUrl.ToString() };

        protected override OAuth2Client.Endpoint UserInfoServiceEndpoint => throw new NotImplementedException();

        protected override UserInfo ParseUserInfo(string content)
        {
            throw new NotImplementedException();
        }
    }
}

using GraphQL.Server.Ui.SmartPlayground.Smart;
using Microsoft.AspNetCore.Http;
using OAuth2.Infrastructure;

namespace GraphQL.Server.Ui.SmartPlayground.Factories
{
    public class SmartClientFactory : ISmartClientFactory
    {
        private const string RedirectPath = "/ui/smartplayground";
        private const string SmartClientId = "dips-smart-graphql-playground";
        private const string SmartScope = "openid";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmartClientFactory(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /*private async Task<SmartClientConfiguration> ProvideSmartConfiguration()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new InvalidOperationException("Failed to get httpContext");
            }

            var authorizeUrl = $@"{httpContext.Request.Scheme}://{httpContext.Request.Host}{SmartConfigurationPath}";

            var message = $"Failed to get SMART config from '{authorizeUrl}'";
            SmartConfiguration? smartConfig;

            try
            {
                smartConfig = await _httpClient.GetFromJsonAsync<SmartConfiguration>(authorizeUrl);
            }
            catch (Exception)
            {
                httpContext.Response.StatusCode = 500;
                throw;
            }

            if (smartConfig == null)
            {
                httpContext.Response.StatusCode = 500;
                throw new InvalidDataException(message);
            }
                        
        }*/

        public ISmartClient CreateClient(SmartPlaygroundOptions options)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                throw new InvalidOperationException("Failed to get httpContext");
            }

            var config = new SmartConfiguration(
                options.AuthorizeUrl,
                options.TokenUrl,
                SmartClientId,
                $@"{httpContext.Request.Scheme}://{httpContext.Request.Host}{RedirectPath}",
                SmartScope);

            var smartClient = new SmartClient(_httpContextAccessor, new RequestFactory(), config);

            return smartClient;
        }
    }
}

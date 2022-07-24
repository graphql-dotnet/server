using GraphQL.Server.Ui.SmartPlayground;
using GraphQL.Server.Ui.SmartPlayground.Smart;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace GraphQL.Server.UI.SmartPlayground.Tests
{
    public class SmartPlaygroundMiddlewareTests
    {
        private readonly SmartPlaygroundMiddleware _smartPlaygroundMiddleware;

        private readonly Mock<Func<ISmartClient>> _smartClientFactoryMock;
        private readonly Mock<ISmartClient> _smartClientMock;
        private readonly Mock<ILogger<SmartPlaygroundMiddleware>> _loggerMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public SmartPlaygroundMiddlewareTests()
        {
            _smartClientMock = new Mock<ISmartClient>();
            _smartClientFactoryMock = new Mock<Func<ISmartClient>>();
            _smartClientFactoryMock.Setup(f => f.Invoke()).Returns(_smartClientMock.Object);

            _loggerMock = new Mock<ILogger<SmartPlaygroundMiddleware>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

            var oauth2Settings = new OAuth2Settings
            {
                BaseUrl = new Uri("https://somepublishedurl")
            };

            var settingsOptionsMock = new Mock<IOptions<OAuth2Settings>>();
            settingsOptionsMock.SetupGet(s => s.Value).Returns(oauth2Settings);

            _smartPlaygroundMiddleware = new SmartPlaygroundMiddleware(
                new Mock<RequestDelegate>().Object,
                new SmartPlaygroundOptions(_loggerFactoryMock.Object, settingsOptionsMock.Object),
                _smartClientFactoryMock.Object);
        }

        [Fact]
        public async Task Invoke_WithLaunchUrlAndNoTokenInCookie_DoesNotDeleteCookieAndCallsLaunchOnSmartClient()
        {
            var requestCookiesMock = new Mock<IRequestCookieCollection>();
            var responseCookiesMock = new Mock<IResponseCookies>();

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.SetupGet(x => x.Path).Returns("/ui/smartplayground/launch");
            httpRequestMock.SetupGet(x => x.Cookies).Returns(requestCookiesMock.Object);

            var httpResponseMock = new Mock<HttpResponse>();
            httpResponseMock.SetupGet(x => x.Cookies).Returns(responseCookiesMock.Object);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(httpRequestMock.Object);

            await _smartPlaygroundMiddleware.InvokeAsync(httpContextMock.Object);

            responseCookiesMock.Verify(c => c.Delete("token"), Times.Never());
            _smartClientMock.Verify(c => c.Launch());
        }

        [Fact]
        public async Task Invoke_WithLaunchUrlAndTokenInCookie_DeletesCookieAndCallsLaunchOnSmartClient()
        {
            var requestCookiesMock = new Mock<IRequestCookieCollection>();
            requestCookiesMock.Setup(c => c["token"]).Returns("some token");
            var responseCookiesMock = new Mock<IResponseCookies>();

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.SetupGet(x => x.Path).Returns("/ui/smartplayground/launch");
            httpRequestMock.SetupGet(x => x.Cookies).Returns(requestCookiesMock.Object);

            var httpResponseMock = new Mock<HttpResponse>();
            httpResponseMock.SetupGet(x => x.Cookies).Returns(responseCookiesMock.Object);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(httpRequestMock.Object);
            httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);


            await _smartPlaygroundMiddleware.InvokeAsync(httpContextMock.Object);

            responseCookiesMock.Verify(c => c.Delete("token"), Times.Once());
            _smartClientMock.Verify(c => c.Launch());
        }

        [Fact]
        public async Task Invoke_WithAppUrlAndCodeParameter_CallsRedirectToGetToken()
        {
            const string SomeCode = "some_code";

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.SetupGet(x => x.Path).Returns("/ui/smartplayground");
            httpRequestMock.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, StringValues> { { "code", SomeCode } }));

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(httpRequestMock.Object);


            await _smartPlaygroundMiddleware.InvokeAsync(httpContextMock.Object);

            _smartClientMock.Verify(c => c.Redirect(SomeCode));
        }

        [Fact]
        public async Task Invoke_WithAppUrlNoCodeParameterAndTokenInCookie_WritesGraphQlPlaygroundPageToBodyStream()
        {
            const string SomeToken = "some_token";

            var requestCookiesMock = new Mock<IRequestCookieCollection>();
            requestCookiesMock.Setup(c => c["token"]).Returns(SomeToken);
            var responseCookiesMock = new Mock<IResponseCookies>();

            var httpRequestMock = new Mock<HttpRequest>();
            httpRequestMock.SetupGet(x => x.Path).Returns("/ui/smartplayground");
            httpRequestMock.SetupGet(x => x.Query).Returns(new QueryCollection());
            httpRequestMock.SetupGet(x => x.Cookies).Returns(requestCookiesMock.Object);

            var bodyStream = new MemoryStream();
            var httpResponseMock = new Mock<HttpResponse>();
            httpResponseMock.Setup(r => r.Body).Returns(bodyStream);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(httpRequestMock.Object);
            httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);


            await _smartPlaygroundMiddleware.InvokeAsync(httpContextMock.Object);

            Assert.True(1 <= bodyStream.Length);
        }
    }
}

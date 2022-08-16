using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Tests;

public class AuthorizationHelperTests
{
    [Fact]
    public async Task NoAuthorizationRequired()
    {
        var ret = await AuthorizationHelper.AuthorizeAsync<object?>(default, null);
        ret.ShouldBeTrue();

        ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(Mock.Of<HttpContext>(MockBehavior.Strict), new GraphQLHttpMiddlewareOptions(), null, null, null), null);
        ret.ShouldBeTrue();
    }

    [Fact]
    public async Task UserNull()
    {
        var options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizationRequired = true;
        var err = await Should.ThrowAsync<InvalidOperationException>(async () => await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(Mock.Of<HttpContext>(), options, null, null, null), null));
        err.Message.ShouldBe("ClaimsPrincipal could not be retrieved from HttpContext.User.");

        options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizedRoles = new List<string> { "test" };
        err = await Should.ThrowAsync<InvalidOperationException>(async () => await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(Mock.Of<HttpContext>(), options, null, null, null), null));
        err.Message.ShouldBe("ClaimsPrincipal could not be retrieved from HttpContext.User.");

        options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizedPolicy = "test";
        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(x => x.GetService(typeof(IAuthorizationService))).Returns(Mock.Of<IAuthorizationService>());
        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(x => x.RequestServices).Returns(mockProvider.Object);
        err = await Should.ThrowAsync<InvalidOperationException>(async () => await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, null), null));
        err.Message.ShouldBe("ClaimsPrincipal could not be retrieved from HttpContext.User.");
    }

    [Fact]
    public async Task IdentityNull()
    {
        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(x => x.User).Returns(new ClaimsPrincipal());

        var options = new GraphQLHttpMiddlewareOptions();
        var ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, null), null);
        ret.ShouldBeTrue();

        options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizationRequired = true;
        var err = await Should.ThrowAsync<InvalidOperationException>(async () => await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, null), null));
        err.Message.ShouldBe("IIdentity could not be retrieved from HttpContext.User.Identity.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authorize(bool expected)
    {
        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(x => x.User).Returns(new ClaimsPrincipal(expected ? new ClaimsIdentity("test") : new ClaimsIdentity()));

        var options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizationRequired = true;
        var ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, null), null);
        ret.ShouldBe(expected);

        bool ranHandler = false;
        ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, async _ => { ranHandler = true; }, null, null), null);
        ret.ShouldBe(expected);
        ranHandler.ShouldBe(!expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorizeRoles(bool expected)
    {
        var mockContext = new Mock<HttpContext>();
        var claims = new List<Claim>();
        if (expected)
            claims.Add(new Claim(ClaimTypes.Role, "test2"));
        mockContext.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")));

        var options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizedRoles.Add("test1");
        options.AuthorizedRoles.Add("test2");
        var ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, null), null);
        ret.ShouldBe(expected);

        bool ranHandler = false;
        ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, async _ => { ranHandler = true; }, null), null);
        ret.ShouldBe(expected);
        ranHandler.ShouldBe(!expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorizePolicy(bool expected)
    {
        var mockContext = new Mock<HttpContext>();
        var user = new ClaimsPrincipal(new ClaimsIdentity("Bearer"));

        var mockAuthorizationService = new Mock<IAuthorizationService>(MockBehavior.Strict);
        mockAuthorizationService.Setup(x => x.AuthorizeAsync(user, null, "test")).Returns(Task.FromResult(expected ? AuthorizationResult.Success() : AuthorizationResult.Failed()));

        var mockProvider = new Mock<IServiceProvider>();
        mockProvider.Setup(x => x.GetService(typeof(IAuthorizationService))).Returns(mockAuthorizationService.Object);
        mockContext.Setup(x => x.RequestServices).Returns(mockProvider.Object);
        mockContext.Setup(x => x.User).Returns(user);

        var options = new GraphQLHttpMiddlewareOptions();
        options.AuthorizedPolicy = "test";
        var ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, null), null);
        ret.ShouldBe(expected);

        bool ranHandler = false;
        ret = await AuthorizationHelper.AuthorizeAsync(new AuthorizationParameters<object?>(mockContext.Object, options, null, null, async (_, _) => { ranHandler = true; }), null);
        ret.ShouldBe(expected);
        ranHandler.ShouldBe(!expected);
    }
}

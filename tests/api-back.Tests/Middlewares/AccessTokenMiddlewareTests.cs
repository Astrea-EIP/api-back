using Microsoft.AspNetCore.Http;
using Moq;
using proto_back.Interfaces.IServices;
using proto_back.Middlewares;

namespace proto_back.Tests.Middlewares;

[Trait("Category", "Unit")]
public class AccessTokenMiddlewareTests
{
    private static (DefaultHttpContext Context, Mock<IAuthService> AuthService, bool[] NextCalled) CreateContext(
        string path, string? accessToken = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (accessToken is not null)
        {
            context.Request.Headers["access-token"] = accessToken;
        }

        return (context, new Mock<IAuthService>(), new bool[1]);
    }

    private static AccessTokenMiddleware WithNext(bool[] nextCalled)
    {
        RequestDelegate next = _ =>
        {
            nextCalled[0] = true;
            return Task.CompletedTask;
        };
        return new AccessTokenMiddleware(next);
    }

    [Theory]
    [InlineData("/v0/auth/anonymous")]
    [InlineData("/V0/AUTH/anonymous")]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task InvokeAsync_PublicPath_CallsNextWithoutHeader(string path)
    {
        var (context, authService, nextCalled) = CreateContext(path);
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.True(nextCalled[0]);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AuthPathWithoutTrailingSlash_Returns401()
    {
        // Characterization: "/v0/auth" itself is not in PublicPrefixes (only "/v0/auth/" is).
        var (context, authService, nextCalled) = CreateContext("/v0/auth");
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.False(nextCalled[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MissingHeader_Returns401WithEmptyBodyAndNextNotCalled()
    {
        var (context, authService, nextCalled) = CreateContext("/v0/itinerary");
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.False(nextCalled[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task InvokeAsync_EmptyHeader_Returns401()
    {
        var (context, authService, nextCalled) = CreateContext("/v0/itinerary", accessToken: "");
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.False(nextCalled[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhitespaceHeader_Returns401()
    {
        var (context, authService, nextCalled) = CreateContext("/v0/itinerary", accessToken: "   ");
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.False(nextCalled[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidToken_Returns401AndValidatesTheGivenToken()
    {
        var (context, authService, nextCalled) = CreateContext("/v0/itinerary", accessToken: "bad-token");
        authService.Setup(a => a.ValidateToken("bad-token")).Returns(false);
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.False(nextCalled[0]);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        authService.Verify(a => a.ValidateToken("bad-token"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ValidToken_CallsNext()
    {
        var (context, authService, nextCalled) = CreateContext("/v0/itinerary", accessToken: "good-token");
        authService.Setup(a => a.ValidateToken("good-token")).Returns(true);
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.True(nextCalled[0]);
    }

    [Fact]
    public async Task InvokeAsync_HeaderNameIsCaseInsensitive()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/v0/itinerary";
        context.Response.Body = new MemoryStream();
        context.Request.Headers["Access-Token"] = "good-token";

        var authService = new Mock<IAuthService>();
        authService.Setup(a => a.ValidateToken("good-token")).Returns(true);
        var nextCalled = new bool[1];
        var middleware = WithNext(nextCalled);

        await middleware.InvokeAsync(context, authService.Object);

        Assert.True(nextCalled[0]);
    }
}

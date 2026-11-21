using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using proto_back.Shared.Auth;

namespace proto_back.Tests.Shared;

[Trait("Category", "Unit")]
public class UserTokenRequiredAttributeTests
{
    private static AuthorizationFilterContext CreateContext(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "access-token"))
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void OnAuthorization_UserToken_LetsTheRequestThrough()
    {
        var context = CreateContext(
            new Claim(AccessTokenClaims.TokenType, AccessTokenClaims.User),
            new Claim(AccessTokenClaims.Subject, "user-42"));

        new UserTokenRequiredAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_AnonymousToken_Returns401()
    {
        var context = CreateContext(new Claim(AccessTokenClaims.TokenType, AccessTokenClaims.Anonymous));

        new UserTokenRequiredAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_UserTokenWithoutUserId_Returns401()
    {
        var context = CreateContext(new Claim(AccessTokenClaims.TokenType, AccessTokenClaims.User));

        new UserTokenRequiredAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_NoClaims_Returns401()
    {
        var context = CreateContext();

        new UserTokenRequiredAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }
}

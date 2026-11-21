using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace proto_back.Shared.Auth;

/// <summary>
/// Restricts a controller or an action to signed-in users: a request made with an anonymous
/// token gets a <c>401</c> with an empty body, like a missing or invalid token.
/// The <c>access-token</c> header itself is checked before, by <c>AccessTokenMiddleware</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UserTokenRequiredAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.IsUserToken())
        {
            context.Result = new UnauthorizedResult();
        }
    }
}

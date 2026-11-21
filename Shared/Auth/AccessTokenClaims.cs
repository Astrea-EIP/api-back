using System.Security.Claims;

namespace proto_back.Shared.Auth;

/// <summary>
/// Claims carried by the access tokens, and helpers to read them from the request user.
/// </summary>
public static class AccessTokenClaims
{
    /// <summary>Claim that tells an anonymous token from a user token.</summary>
    public const string TokenType = "token_type";

    /// <summary>Value of <see cref="TokenType"/> for a token from <c>GET /v0/auth/anonymous</c>.</summary>
    public const string Anonymous = "anonymous";

    /// <summary>Value of <see cref="TokenType"/> for a token issued to a signed-in user.</summary>
    public const string User = "user";

    /// <summary>Claim holding the user id (standard JWT <c>sub</c> claim).</summary>
    public const string Subject = "sub";

    /// <summary>Whether the request was made with a user token.</summary>
    public static bool IsUserToken(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(TokenType) == User
        && !string.IsNullOrEmpty(principal.FindFirstValue(Subject));

    /// <summary>The id of the signed-in user, or <c>null</c> for an anonymous token.</summary>
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.IsUserToken() ? principal.FindFirstValue(Subject) : null;
}

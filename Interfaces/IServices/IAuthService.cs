using System.Security.Claims;

namespace proto_back.Interfaces.IServices;

public interface IAuthService
{
    /// <summary>Create a signed token for a visitor without an account.</summary>
    string GenerateAnonymousToken();

    /// <summary>Create a signed token for the user with this id.</summary>
    string GenerateUserToken(string userId);

    /// <summary>
    /// Check the signature, issuer, audience and expiry of a token.
    /// Returns its claims, or <c>null</c> when the token is not valid.
    /// </summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
}

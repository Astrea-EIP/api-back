namespace proto_back.DTOs.Responses;

/// <summary>
/// Response body for <c>GET /v0/auth/anonymous</c>, <c>POST /v0/auth/register</c> and
/// <c>POST /v0/auth/login</c>. The expiry is in the <c>exp</c> claim of the token.
/// </summary>
public class TokenResponse
{
    /// <summary>The access token (JWT). Send it as the <c>access-token</c> header on every other request.</summary>
    public string AccessToken { get; set; } = null!;
}

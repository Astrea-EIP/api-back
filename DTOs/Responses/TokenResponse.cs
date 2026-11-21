namespace proto_back.DTOs.Responses;

/// <summary>
/// Response body for <c>GET /v0/auth/anonymous</c>.
/// </summary>
public class TokenResponse
{
    /// <summary>The access token (JWT). Send it as the <c>access-token</c> header on every other request.</summary>
    public string AccessToken { get; set; } = null!;
}

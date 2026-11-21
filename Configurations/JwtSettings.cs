namespace proto_back.Configurations;

/// <summary>
/// Settings for the access tokens (JWT), bound from the <c>Jwt</c> configuration section.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret used to sign the tokens (HMAC SHA-256), at least 32 characters.
    /// Set it with the <c>Jwt__SigningKey</c> environment variable. When it is missing, the API
    /// signs with a random key generated at startup: tokens then stop working when the API restarts.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>Value of the <c>iss</c> claim.</summary>
    public string Issuer { get; set; } = "astrea-api";

    /// <summary>Value of the <c>aud</c> claim.</summary>
    public string Audience { get; set; } = "astrea-apps";

    /// <summary>Lifetime of an anonymous token, in minutes.</summary>
    public int AnonymousTokenLifetimeMinutes { get; set; } = 1440;

    /// <summary>Lifetime of a user token, in minutes.</summary>
    public int UserTokenLifetimeMinutes { get; set; } = 10080;
}

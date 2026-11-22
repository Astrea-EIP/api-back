using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using proto_back.Configurations;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Auth;

namespace proto_back.Services;

/// <summary>
/// Issues and checks the access tokens: JWT signed with HMAC SHA-256. Nothing is stored,
/// so a token stays valid after a restart as long as the signing key does not change.
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>Minimum length of <see cref="JwtSettings.SigningKey"/> (256 bits for HMAC SHA-256).</summary>
    public const int MinimumSigningKeyLength = 32;

    private readonly JwtSettings _settings;
    private readonly SymmetricSecurityKey _key;
    private readonly JsonWebTokenHandler _handler = new();
    private readonly TimeProvider _timeProvider;

    public AuthService(IOptions<JwtSettings> options, ILogger<AuthService> logger, TimeProvider? timeProvider = null)
    {
        _settings = options.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (string.IsNullOrEmpty(_settings.SigningKey))
        {
            logger.LogWarning(
                "Jwt:SigningKey is not set: access tokens are signed with a random key and stop working when the API restarts.");
            _key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(MinimumSigningKeyLength));
        }
        else if (_settings.SigningKey.Length < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumSigningKeyLength} characters long.");
        }
        else
        {
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        }
    }

    public string GenerateAnonymousToken() =>
        CreateToken(
            new Dictionary<string, object> { [AccessTokenClaims.TokenType] = AccessTokenClaims.Anonymous },
            _settings.AnonymousTokenLifetimeMinutes);

    public string GenerateUserToken(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return CreateToken(
            new Dictionary<string, object>
            {
                [AccessTokenClaims.TokenType] = AccessTokenClaims.User,
                [AccessTokenClaims.Subject] = userId
            },
            _settings.UserTokenLifetimeMinutes);
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = _key,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.FromSeconds(30),
            LifetimeValidator = (notBefore, expires, _, parameters) =>
                expires is not null && _timeProvider.GetUtcNow().UtcDateTime < expires.Value + parameters.ClockSkew
        });

        if (!result.IsValid)
        {
            return null;
        }

        var tokenType = result.ClaimsIdentity.FindFirst(AccessTokenClaims.TokenType)?.Value;
        return tokenType is AccessTokenClaims.Anonymous or AccessTokenClaims.User
            ? new ClaimsPrincipal(result.ClaimsIdentity)
            : null;
    }

    private string CreateToken(Dictionary<string, object> claims, int lifetimeMinutes)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        claims[JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N");

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Claims = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(lifetimeMinutes),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        });
    }
}

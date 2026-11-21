using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using proto_back.Configurations;
using proto_back.Services;
using proto_back.Shared.Auth;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class AuthServiceTests
{
    private const string SigningKey = "a-unit-test-signing-key-of-at-least-32-chars";

    private static AuthService CreateService(
        string? signingKey = SigningKey,
        TimeProvider? timeProvider = null,
        Action<JwtSettings>? configure = null)
    {
        var settings = new JwtSettings { SigningKey = signingKey };
        configure?.Invoke(settings);
        return new AuthService(Options.Create(settings), NullLogger<AuthService>.Instance, timeProvider);
    }

    [Fact]
    public async Task GenerateAnonymousToken_Validated_IsAnonymousWithoutUserId()
    {
        var service = CreateService();

        var principal = await service.ValidateTokenAsync(service.GenerateAnonymousToken());

        Assert.NotNull(principal);
        Assert.Equal(AccessTokenClaims.Anonymous, principal!.FindFirst(AccessTokenClaims.TokenType)?.Value);
        Assert.False(principal.IsUserToken());
        Assert.Null(principal.GetUserId());
    }

    [Fact]
    public async Task GenerateUserToken_Validated_IsUserTokenWithTheUserId()
    {
        var service = CreateService();

        var principal = await service.ValidateTokenAsync(service.GenerateUserToken("user-42"));

        Assert.NotNull(principal);
        Assert.True(principal!.IsUserToken());
        Assert.Equal("user-42", principal.GetUserId());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateUserToken_BlankUserId_Throws(string userId)
    {
        var service = CreateService();

        Assert.ThrowsAny<ArgumentException>(() => service.GenerateUserToken(userId));
    }

    [Fact]
    public void GenerateAnonymousToken_CalledTwice_ReturnsDifferentTokens()
    {
        var service = CreateService();

        Assert.NotEqual(service.GenerateAnonymousToken(), service.GenerateAnonymousToken());
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenFromAnotherInstanceWithSameKey_IsValid()
    {
        var token = CreateService().GenerateAnonymousToken();

        var principal = await CreateService().ValidateTokenAsync(token);

        Assert.NotNull(principal);
    }

    [Fact]
    public async Task ValidateTokenAsync_TokenSignedWithAnotherKey_ReturnsNull()
    {
        var token = CreateService(signingKey: "another-unit-test-signing-key-of-32-chars").GenerateAnonymousToken();

        var principal = await CreateService().ValidateTokenAsync(token);

        Assert.Null(principal);
    }

    [Fact]
    public async Task ValidateTokenAsync_NoSigningKeyConfigured_TokensOnlyValidForTheSameInstance()
    {
        var first = CreateService(signingKey: null);
        var second = CreateService(signingKey: null);
        var token = first.GenerateAnonymousToken();

        Assert.NotNull(await first.ValidateTokenAsync(token));
        Assert.Null(await second.ValidateTokenAsync(token));
    }

    [Fact]
    public void Constructor_SigningKeyShorterThan32Characters_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CreateService(signingKey: "too-short"));
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredAnonymousToken_ReturnsNull()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = CreateService(timeProvider: time);
        var token = service.GenerateAnonymousToken();

        time.Advance(TimeSpan.FromMinutes(new JwtSettings().AnonymousTokenLifetimeMinutes + 1));

        Assert.Null(await service.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateTokenAsync_UserTokenBeforeExpiry_IsValid()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var service = CreateService(timeProvider: time);
        var token = service.GenerateUserToken("user-42");

        time.Advance(TimeSpan.FromMinutes(new JwtSettings().UserTokenLifetimeMinutes - 1));

        Assert.NotNull(await service.ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateTokenAsync_OtherAudience_ReturnsNull()
    {
        var token = CreateService(configure: s => s.Audience = "someone-else").GenerateAnonymousToken();

        Assert.Null(await CreateService().ValidateTokenAsync(token));
    }

    [Fact]
    public async Task ValidateTokenAsync_TamperedPayload_ReturnsNull()
    {
        var service = CreateService();
        var parts = service.GenerateUserToken("user-42").Split('.');
        var otherPayload = service.GenerateUserToken("user-43").Split('.')[1];

        var principal = await service.ValidateTokenAsync($"{parts[0]}.{otherPayload}.{parts[2]}");

        Assert.Null(principal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("0123456789abcdef0123456789abcdef")]
    public async Task ValidateTokenAsync_NotAValidToken_ReturnsNull(string token)
    {
        Assert.Null(await CreateService().ValidateTokenAsync(token));
    }

    [Fact]
    public async Task GenerateAnonymousToken_OneThousandTokensInParallel_AreAllDistinctAndValid()
    {
        var service = CreateService();

        var tokens = Enumerable.Range(0, 1000)
            .AsParallel()
            .Select(_ => service.GenerateAnonymousToken())
            .ToList();

        Assert.Equal(1000, tokens.Distinct().Count());
        foreach (var token in tokens)
        {
            Assert.NotNull(await service.ValidateTokenAsync(token));
        }
    }
}

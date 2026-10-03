using System.Text.RegularExpressions;
using proto_back.Services;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class AuthServiceTests
{
    private static readonly Regex TokenFormat = new("^[0-9a-f]{32}$");

    [Fact]
    public void GenerateAnonymousToken_Always_ReturnsLowercase32HexCharacters()
    {
        var service = new AuthService();

        var token = service.GenerateAnonymousToken();

        Assert.Matches(TokenFormat, token);
    }

    [Fact]
    public void GenerateAnonymousToken_CalledTwice_ReturnsDifferentTokens()
    {
        var service = new AuthService();

        var first = service.GenerateAnonymousToken();
        var second = service.GenerateAnonymousToken();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ValidateToken_GeneratedToken_ReturnsTrue()
    {
        var service = new AuthService();
        var token = service.GenerateAnonymousToken();

        var isValid = service.ValidateToken(token);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_UnknownToken_ReturnsFalse()
    {
        var service = new AuthService();

        var isValid = service.ValidateToken(Guid.NewGuid().ToString("N"));

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsFalse()
    {
        var service = new AuthService();

        var isValid = service.ValidateToken(string.Empty);

        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_TokenFromAnotherInstance_ReturnsFalse()
    {
        var first = new AuthService();
        var second = new AuthService();
        var token = first.GenerateAnonymousToken();

        var isValid = second.ValidateToken(token);

        Assert.False(isValid);
    }

    [Fact]
    public void GenerateAnonymousToken_OneThousandTokensInParallel_AreAllDistinctAndValid()
    {
        var service = new AuthService();

        var tokens = Enumerable.Range(0, 1000)
            .AsParallel()
            .Select(_ => service.GenerateAnonymousToken())
            .ToList();

        Assert.Equal(1000, tokens.Distinct().Count());
        Assert.All(tokens, token => Assert.True(service.ValidateToken(token)));
    }
}

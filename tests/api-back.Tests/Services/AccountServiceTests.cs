using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Time.Testing;
using Moq;
using proto_back.DTOs.Requests;
using proto_back.Interfaces.IServices;
using proto_back.Models.Entities;
using proto_back.Services;
using proto_back.Shared.Errors;
using proto_back.Tests.Support;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class AccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 14, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUserRepository _users = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _authService.Setup(a => a.GenerateUserToken(It.IsAny<string>())).Returns<string>(id => "token-for-" + id);
        _service = new AccountService(_users, _authService.Object, new PasswordHasher<User>(), new FakeTimeProvider(Now));
    }

    private static RegisterRequest NewRegisterRequest(string email = "Camille@Example.com ") => new()
    {
        Name = "  Camille ",
        Email = email,
        Password = "correct-horse-battery-9",
        BirthDate = new DateOnly(1998, 4, 12)
    };

    [Fact]
    public async Task RegisterAsync_NewEmail_StoresTheAccountAndReturnsAUserTokenForIt()
    {
        var token = await _service.RegisterAsync(NewRegisterRequest());

        var user = Assert.Single(_users.Users);
        Assert.Equal("token-for-" + user.Id, token);
        Assert.Equal("Camille", user.Name);
        Assert.Equal("camille@example.com", user.Email);
        Assert.Equal(new DateTime(1998, 4, 12, 0, 0, 0, DateTimeKind.Utc), user.BirthDate);
        Assert.Equal(Now.UtcDateTime, user.CreatedAt);
    }

    [Fact]
    public async Task RegisterAsync_Always_StoresAHashAndNeverThePassword()
    {
        await _service.RegisterAsync(NewRegisterRequest());

        var user = Assert.Single(_users.Users);
        Assert.DoesNotContain("correct-horse-battery-9", user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, "correct-horse-battery-9"));
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyUsedWithAnotherCase_ThrowsEmailAlreadyUsedException()
    {
        await _service.RegisterAsync(NewRegisterRequest("camille@example.com"));

        var ex = await Assert.ThrowsAsync<EmailAlreadyUsedException>(
            () => _service.RegisterAsync(NewRegisterRequest("CAMILLE@example.com")));

        Assert.Equal(409, ex.StatusCode);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task LoginAsync_RightPasswordAndEmailWithAnotherCase_ReturnsAUserTokenForTheAccount()
    {
        await _service.RegisterAsync(NewRegisterRequest());
        var user = Assert.Single(_users.Users);

        var token = await _service.LoginAsync(new LoginRequest { Email = " CAMILLE@example.com", Password = "correct-horse-battery-9" });

        Assert.Equal("token-for-" + user.Id, token);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        await _service.RegisterAsync(NewRegisterRequest());

        var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest { Email = "camille@example.com", Password = "wrong-password" }));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsTheSameErrorAsAWrongPassword()
    {
        await _service.RegisterAsync(NewRegisterRequest());
        var wrongPassword = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest { Email = "camille@example.com", Password = "wrong-password" }));

        var unknownEmail = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "correct-horse-battery-9" }));

        Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_StillChecksAPassword()
    {
        var hasher = new Mock<IPasswordHasher<User>>();
        hasher.Setup(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>())).Returns("dummy-hash");
        var service = new AccountService(_users, _authService.Object, hasher.Object);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "whatever-password" }));

        hasher.Verify(h => h.VerifyHashedPassword(It.IsAny<User>(), "dummy-hash", "whatever-password"), Times.Once);
    }
}

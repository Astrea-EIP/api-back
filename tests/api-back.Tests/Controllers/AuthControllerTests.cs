using Microsoft.AspNetCore.Mvc;
using Moq;
using proto_back.Controllers;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;

namespace proto_back.Tests.Controllers;

[Trait("Category", "Unit")]
public class AuthControllerTests
{
    [Fact]
    public void GetAnonymousToken_Always_ReturnsAcceptedWithServiceToken()
    {
        var authService = new Mock<IAuthService>();
        authService.Setup(a => a.GenerateAnonymousToken()).Returns("abc123");
        var controller = new AuthController(authService.Object, Mock.Of<IAccountService>());

        var result = controller.GetAnonymousToken();

        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = Assert.IsType<TokenResponse>(accepted.Value);
        Assert.Equal("abc123", body.AccessToken);
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201WithUserToken()
    {
        var request = new RegisterRequest { Name = "Camille", Email = "camille@example.com", Password = "correct-horse-battery", BirthDate = new DateOnly(1998, 4, 12) };
        var accountService = new Mock<IAccountService>();
        accountService.Setup(a => a.RegisterAsync(request)).ReturnsAsync("user-token");
        var controller = new AuthController(Mock.Of<IAuthService>(), accountService.Object);

        var result = await controller.Register(request);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal("user-token", Assert.IsType<TokenResponse>(created.Value).AccessToken);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithUserToken()
    {
        var request = new LoginRequest { Email = "camille@example.com", Password = "correct-horse-battery" };
        var accountService = new Mock<IAccountService>();
        accountService.Setup(a => a.LoginAsync(request)).ReturnsAsync("user-token");
        var controller = new AuthController(Mock.Of<IAuthService>(), accountService.Object);

        var result = await controller.Login(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("user-token", Assert.IsType<TokenResponse>(ok.Value).AccessToken);
    }
}

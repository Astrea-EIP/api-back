using Microsoft.AspNetCore.Mvc;
using Moq;
using proto_back.Controllers;
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
        var controller = new AuthController(authService.Object);

        var result = controller.GetAnonymousToken();

        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = Assert.IsType<TokenResponse>(accepted.Value);
        Assert.Equal("abc123", body.AccessToken);
    }
}

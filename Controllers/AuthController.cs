using Microsoft.AspNetCore.Mvc;
using proto_back.Interfaces.IServices;

namespace proto_back.Controllers;

[ApiController]
[Route("v0/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Get an anonymous access token.
    /// </summary>
    [HttpGet("anonymous")]
    [ProducesResponseType(typeof(string), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Shared.Errors.ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Shared.Errors.ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAnonymousToken()
    {
        var token = _authService.GenerateAnonymousToken();
        return Accepted(token);
    }
}

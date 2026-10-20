using Microsoft.AspNetCore.Mvc;
using proto_back.Interfaces.IServices;

namespace proto_back.Controllers;

/// <summary>
/// Issues anonymous access tokens.
/// </summary>
[ApiController]
[Route("v0/auth")]
[Produces("application/json")]
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
    /// <remarks>This endpoint is public: no <c>access-token</c> header is required. The returned token must be sent as the <c>access-token</c> header on every other request. Tokens are kept in memory and are lost when the API restarts.</remarks>
    /// <response code="202">A new anonymous access token was generated.</response>
    /// <response code="400">The request could not be processed.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("anonymous", Name = "getAnonymousToken")]
    [ProducesResponseType(typeof(proto_back.DTOs.Responses.TokenResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(Shared.Errors.ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Shared.Errors.ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAnonymousToken()
    {
        var token = _authService.GenerateAnonymousToken();
        var response = new proto_back.DTOs.Responses.TokenResponse { AccessToken = token };
        return Accepted(response);
    }
}

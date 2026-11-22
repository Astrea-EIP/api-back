using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Auth;
using proto_back.Shared.Errors;

namespace proto_back.Controllers;

/// <summary>
/// Issues access tokens: anonymous tokens, and user tokens on sign-up and sign-in.
/// </summary>
[ApiController]
[Route("v0/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAccountService _accountService;

    public AuthController(IAuthService authService, IAccountService accountService)
    {
        _authService = authService;
        _accountService = accountService;
    }

    /// <summary>
    /// Get an anonymous access token.
    /// </summary>
    /// <remarks>This endpoint is public: no <c>access-token</c> header is required. The returned token must be sent as the <c>access-token</c> header on every other request. The token is a signed JWT that expires after 24 hours: on a <c>401</c>, get a new one.</remarks>
    /// <response code="202">A new anonymous access token was generated.</response>
    /// <response code="400">The request could not be processed.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("anonymous", Name = "getAnonymousToken")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult GetAnonymousToken()
    {
        var token = _authService.GenerateAnonymousToken();
        var response = new TokenResponse { AccessToken = token };
        return Accepted(response);
    }

    /// <summary>
    /// Create an account and sign in.
    /// </summary>
    /// <remarks>This endpoint is public: no <c>access-token</c> header is required. No consent is asked here: the account holds no health data. The returned token is a user token, sent as the <c>access-token</c> header like the anonymous token. It expires after 7 days: on a <c>401</c>, sign in again.</remarks>
    /// <response code="201">The account was created and the user is signed in.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="409">An account already exists with this email address.</response>
    /// <response code="429">Too many sign-ups from this IP address. Wait for the number of seconds in the <c>Retry-After</c> header.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPost("register", Name = "register")]
    [EnableRateLimiting(AuthRateLimiting.RegisterPolicy)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var token = await _accountService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, new TokenResponse { AccessToken = token });
    }

    /// <summary>
    /// Sign in with an email address and a password.
    /// </summary>
    /// <remarks>This endpoint is public: no <c>access-token</c> header is required. The returned token is a user token that expires after 7 days.</remarks>
    /// <response code="200">The user is signed in.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="401">The email address or the password is wrong. The message does not say which one, so that it cannot be used to find out whether an email address has an account.</response>
    /// <response code="429">Too many sign-ins from this IP address, or too many failed sign-ins for this email address. Wait for the number of seconds in the <c>Retry-After</c> header.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPost("login", Name = "login")]
    [EnableRateLimiting(AuthRateLimiting.LoginPolicy)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await _accountService.LoginAsync(request);
        return Ok(new TokenResponse { AccessToken = token });
    }
}

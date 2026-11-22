using Microsoft.AspNetCore.Mvc;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Auth;
using proto_back.Shared.Errors;

namespace proto_back.Controllers;

/// <summary>
/// The signed-in user's account and mobility profile. Every route needs a user token, and
/// answers <c>401</c> when the account of the token no longer exists.
/// </summary>
[ApiController]
[Route("v0/users/me")]
[Produces("application/json")]
[UserTokenRequired]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private string UserId => User.GetUserId()!;

    /// <summary>
    /// Get the signed-in user's account.
    /// </summary>
    /// <remarks>Requires a user token from <c>POST /v0/auth/register</c> or <c>POST /v0/auth/login</c>.</remarks>
    /// <response code="200">The account of the user the token belongs to.</response>
    /// <response code="401">The access-token header is missing, invalid, expired, or is an anonymous token, or the account of the user token no longer exists.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet(Name = "getCurrentUser")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var account = await _userService.GetAccountAsync(UserId);
        return account is null ? Unauthorized() : Ok(account);
    }

    /// <summary>
    /// Delete the signed-in user's account and mobility profile.
    /// </summary>
    /// <remarks>Right to erasure (GDPR): the account and the mobility profile are deleted for good. Tokens are stateless and are not revoked, but every <c>/v0/users/me</c> route checks that the account still exists and answers <c>401</c> otherwise, so the user token gives no access to personal data any more. It can still call the anonymous routes until it expires, like an anonymous token.</remarks>
    /// <response code="204">The account and the mobility profile were deleted.</response>
    /// <response code="401">The access-token header is missing, invalid, expired, or is an anonymous token, or the account of the user token no longer exists.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpDelete(Name = "deleteCurrentUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCurrentUser()
    {
        return await _userService.DeleteAccountAsync(UserId) ? NoContent() : Unauthorized();
    }

    /// <summary>
    /// Get the signed-in user's mobility profile.
    /// </summary>
    /// <response code="200">The saved mobility profile.</response>
    /// <response code="401">The access-token header is missing, invalid, expired, or is an anonymous token, or the account of the user token no longer exists.</response>
    /// <response code="404">The user has not saved a mobility profile yet.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet("profile", Name = "getCurrentUserProfile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        if (!await _userService.AccountExistsAsync(UserId))
        {
            return Unauthorized();
        }

        var profile = await _userService.GetProfileAsync(UserId);
        return profile is null
            ? NotFound(new ErrorResponse { Error = "The user has not saved a mobility profile yet." })
            : Ok(profile);
    }

    /// <summary>
    /// Create or replace the signed-in user's mobility profile.
    /// </summary>
    /// <remarks>The whole profile is replaced. <c>consent</c> must be <c>true</c> (GDPR: the profile is health data). Fields that do not apply to the chosen <c>mobility_profile</c> are ignored and not stored, with the same rules as <c>POST /v0/itinerary</c>.</remarks>
    /// <response code="200">The profile was saved.</response>
    /// <response code="400">A field is invalid, the consent was not given, or the wheelchair model is not in the catalog.</response>
    /// <response code="401">The access-token header is missing, invalid, expired, or is an anonymous token, or the account of the user token no longer exists.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPut("profile", Name = "saveCurrentUserProfile")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SaveCurrentUserProfile([FromBody] UserProfileRequest request)
    {
        if (!await _userService.AccountExistsAsync(UserId))
        {
            return Unauthorized();
        }

        return Ok(await _userService.SaveProfileAsync(UserId, request));
    }

    /// <summary>
    /// Delete the signed-in user's mobility profile and withdraw the consent.
    /// </summary>
    /// <remarks>Withdrawal of the consent (GDPR): the mobility profile and the consent date are deleted for good, the account is kept. The user can save a new profile later with <c>PUT /v0/users/me/profile</c>, which asks for the consent again. Deleting a profile that does not exist also answers <c>204</c>.</remarks>
    /// <response code="204">The mobility profile and the consent were deleted.</response>
    /// <response code="401">The access-token header is missing, invalid, expired, or is an anonymous token, or the account of the user token no longer exists.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpDelete("profile", Name = "deleteCurrentUserProfile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCurrentUserProfile()
    {
        if (!await _userService.AccountExistsAsync(UserId))
        {
            return Unauthorized();
        }

        await _userService.DeleteProfileAsync(UserId);
        return NoContent();
    }
}

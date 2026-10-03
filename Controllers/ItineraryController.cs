using Microsoft.AspNetCore.Mvc;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Errors;

namespace proto_back.Controllers;

/// <summary>
/// Computes accessibility-aware itineraries.
/// </summary>
[ApiController]
[Route("v0/itinerary")]
[Produces("application/json")]
public class ItineraryController : ControllerBase
{
    private readonly IItineraryService _itineraryService;

    public ItineraryController(IItineraryService itineraryService)
    {
        _itineraryService = itineraryService;
    }

    /// <summary>
    /// Compute an itinerary between two points.
    /// </summary>
    /// <remarks>
    /// Requires the <c>access-token</c> header (see <c>GET /v0/auth/anonymous</c>).
    /// </remarks>
    /// <param name="accessToken">The access-token header (unused in code; enforced by AccessTokenMiddleware).</param>
    /// <param name="request">Start/end points and the mobility profile to route for.</param>
    /// <response code="201">The itinerary was computed successfully.</response>
    /// <response code="400">The request body is invalid.</response>
    /// <response code="401">The access-token header is missing or invalid.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPost(Name = "createItinerary")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostItinerary(
        [FromHeader(Name = "access-token")] string accessToken,
        [FromBody] CreateItineraryRequest request)
    {
        var result = await _itineraryService.ComputeItineraryAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

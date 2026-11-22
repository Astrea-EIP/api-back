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
    /// <param name="request">Start/end points and the mobility profile to route for.</param>
    /// <response code="201">The itinerary was computed successfully.</response>
    /// <response code="400">The request body is invalid, or an address cannot be found.</response>
    /// <response code="401">The access-token header is missing or invalid.</response>
    /// <response code="422">A point is outside the area covered by the map, or no route matches the mobility profile.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpPost(Name = "createItinerary")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PostItinerary([FromBody] CreateItineraryRequest request)
    {
        var result = await _itineraryService.ComputeItineraryAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Compute an itinerary between two points, with the parameters in the query string.
    /// </summary>
    /// <remarks>
    /// Same computation as <c>POST /v0/itinerary</c>, for clients that can only send a GET
    /// (shareable link, simple fetch). The query parameters use the same names and formats as
    /// the POST body fields. Requires the <c>access-token</c> header (see <c>GET /v0/auth/anonymous</c>).
    /// </remarks>
    /// <param name="request">Start/end points and the mobility profile to route for.</param>
    /// <response code="200">The itinerary was computed successfully.</response>
    /// <response code="400">A query parameter is missing or invalid, or an address cannot be found.</response>
    /// <response code="401">The access-token header is missing or invalid.</response>
    /// <response code="422">A point is outside the area covered by the map, or no route matches the mobility profile.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet(Name = "getItinerary")]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetItinerary([FromQuery] CreateItineraryRequest request)
    {
        var result = await _itineraryService.ComputeItineraryAsync(request);
        return Ok(result);
    }
}

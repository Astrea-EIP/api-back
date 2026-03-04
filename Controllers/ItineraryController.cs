using Microsoft.AspNetCore.Mvc;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Errors;

namespace proto_back.Controllers;

[ApiController]
[Route("v0/itinerary")]
public class ItineraryController : ControllerBase
{
    private readonly IItineraryService _itineraryService;

    public ItineraryController(IItineraryService itineraryService)
    {
        _itineraryService = itineraryService;
    }

    /// <summary>
    /// Compute an itinerary between two points.
    /// Requires the access-token header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ItineraryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult PostItinerary([FromBody] CreateItineraryRequest request)
    {
        var result = _itineraryService.ComputeItinerary(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

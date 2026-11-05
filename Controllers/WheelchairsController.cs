using Microsoft.AspNetCore.Mvc;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Errors;

namespace proto_back.Controllers;

/// <summary>
/// Exposes the catalogue of standard wheelchairs.
/// </summary>
[ApiController]
[Route("v0/wheelchairs")]
[Produces("application/json")]
public class WheelchairsController : ControllerBase
{
    private readonly IWheelchairService _wheelchairService;

    public WheelchairsController(IWheelchairService wheelchairService)
    {
        _wheelchairService = wheelchairService;
    }

    /// <summary>
    /// List the standard wheelchairs and the accessibility reference values.
    /// </summary>
    /// <remarks>
    /// Requires the <c>access-token</c> header (see <c>GET /v0/auth/anonymous</c>).
    /// Use it to build the wheelchair selector, then send the chosen model's
    /// <c>recommended_wheelchair_width</c> as <c>wheelchair_width</c> to <c>POST /v0/itinerary</c>.
    /// All dimensions are in meters.
    /// </remarks>
    /// <response code="200">The wheelchair catalogue.</response>
    /// <response code="401">The access-token header is missing or invalid.</response>
    /// <response code="500">An unexpected error occurred.</response>
    [HttpGet(Name = "listWheelchairs")]
    [ProducesResponseType(typeof(WheelchairCatalogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ServerErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<WheelchairCatalogResponse> GetWheelchairs()
    {
        return Ok(_wheelchairService.GetCatalog());
    }
}

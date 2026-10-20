using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// Response body for <c>POST /v0/itinerary</c>.
/// </summary>
public class ItineraryResponse
{
    /// <summary>
    /// The computed route, as an ordered list of points from start to end.
    /// </summary>
    [JsonPropertyName("points")]
    public List<PointResponse> Points { get; set; } = new();
}

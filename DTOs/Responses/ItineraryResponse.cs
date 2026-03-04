using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

public class ItineraryResponse
{
    [JsonPropertyName("points")]
    public List<PointResponse> Points { get; set; } = new();
}

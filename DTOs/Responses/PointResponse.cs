using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

public class PointResponse
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// A geographic point.
/// </summary>
public class PointResponse
{
    /// <summary>Latitude.</summary>
    /// <example>48.8566</example>
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    /// <summary>Longitude.</summary>
    /// <example>2.3522</example>
    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}

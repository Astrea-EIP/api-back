using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// A dimension range, in meters.
/// </summary>
public class DimensionRangeResponse
{
    /// <summary>Smallest value, in meters.</summary>
    /// <example>0.55</example>
    [JsonPropertyName("min")]
    public double Min { get; set; }

    /// <summary>Largest value, in meters.</summary>
    /// <example>0.7</example>
    [JsonPropertyName("max")]
    public double Max { get; set; }
}

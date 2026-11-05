using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// Spatial accessibility reference values, in meters.
/// </summary>
public class AccessibilityStandardsResponse
{
    /// <summary>Minimum free passage width, in meters.</summary>
    /// <example>0.8</example>
    [JsonPropertyName("min_passage_width")]
    public double MinPassageWidth { get; set; }

    /// <summary>Recommended free passage width in corridors, in meters.</summary>
    /// <example>0.9</example>
    [JsonPropertyName("recommended_corridor_width")]
    public double RecommendedCorridorWidth { get; set; }

    /// <summary>Diameter of the circle needed to turn around, in meters.</summary>
    /// <example>1.5</example>
    [JsonPropertyName("turning_circle_diameter")]
    public double TurningCircleDiameter { get; set; }
}

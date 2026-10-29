using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// A standard wheelchair model (brand and range) with its overall dimensions.
/// </summary>
public class WheelchairModelResponse
{
    /// <summary>Stable identifier of the model. Store this, not the label, to remember the user's choice.</summary>
    /// <example>invacare-action-kuschall</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>Manufacturer.</summary>
    /// <example>Invacare</example>
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = null!;

    /// <summary>Product ranges covered by this entry.</summary>
    /// <example>Action, Küschall</example>
    [JsonPropertyName("range")]
    public string Range { get; set; } = null!;

    /// <summary>Overall width range, in meters.</summary>
    [JsonPropertyName("width")]
    public DimensionRangeResponse Width { get; set; } = null!;

    /// <summary>Overall length range, in meters. Informational: the engine does not use it yet.</summary>
    [JsonPropertyName("length")]
    public DimensionRangeResponse Length { get; set; } = null!;

    /// <summary>Width in meters to send as <c>wheelchair_width</c> to <c>POST /v0/itinerary</c>. It is the widest chair of the range, so the returned path fits every chair of the range.</summary>
    /// <example>0.7</example>
    [JsonPropertyName("recommended_wheelchair_width")]
    public double RecommendedWheelchairWidth { get; set; }
}

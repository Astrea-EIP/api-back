using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// A wheelchair category and its standard models.
/// </summary>
public class WheelchairCategoryResponse
{
    /// <summary>Stable identifier of the category, to use as a translation key on the client.</summary>
    /// <example>manual</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>English display name of the category.</summary>
    /// <example>Manual wheelchairs</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>The standard models of this category.</summary>
    [JsonPropertyName("models")]
    public List<WheelchairModelResponse> Models { get; set; } = new();
}

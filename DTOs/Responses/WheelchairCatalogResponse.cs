using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>
/// Response body for <c>GET /v0/wheelchairs</c>.
/// </summary>
public class WheelchairCatalogResponse
{
    /// <summary>The wheelchair categories (manual, electric), each with its standard models.</summary>
    [JsonPropertyName("categories")]
    public List<WheelchairCategoryResponse> Categories { get; set; } = new();

    /// <summary>Reference values for designing navigable routes and spaces.</summary>
    [JsonPropertyName("accessibility_standards")]
    public AccessibilityStandardsResponse AccessibilityStandards { get; set; } = null!;
}

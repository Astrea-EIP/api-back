using System.Text.Json.Serialization;
using proto_back.DTOs.Requests;

namespace proto_back.DTOs.Responses;

/// <summary>
/// Response body for <c>GET</c> and <c>PUT /v0/users/me/profile</c>: the saved mobility profile.
/// Only the fields that apply to <c>mobility_profile</c> are present.
/// </summary>
public class UserProfileResponse
{
    /// <summary>Always <c>true</c>: a profile is only stored with the consent.</summary>
    [JsonPropertyName("consent")]
    public bool Consent { get; set; } = true;

    /// <example>manual-wheelchair</example>
    [JsonPropertyName("app_profile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AppProfile { get; set; }

    [JsonPropertyName("mobility_profile")]
    public MobilityProfile MobilityProfile { get; set; }

    /// <example>tilite-active</example>
    [JsonPropertyName("wheelchair_model_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WheelchairModelId { get; set; }

    /// <example>0.7</example>
    [JsonPropertyName("wheelchair_width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? WheelchairWidth { get; set; }

    [JsonPropertyName("physical_strength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhysicalStrength? PhysicalStrength { get; set; }

    [JsonPropertyName("is_accompanied")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsAccompanied { get; set; }

    [JsonPropertyName("can_climb_stairs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CanClimbStairs { get; set; }

    [JsonPropertyName("path_preferences")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PathPreference? PathPreferences { get; set; }

    /// <summary>When the user gave the consent to store the profile. Kept when the profile is replaced; set again after a withdrawal.</summary>
    [JsonPropertyName("consent_given_at")]
    public DateTime ConsentGivenAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

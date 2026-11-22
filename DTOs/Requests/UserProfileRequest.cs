using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using proto_back.Shared.Validation;

namespace proto_back.DTOs.Requests;

/// <summary>
/// Request body for <c>PUT /v0/users/me/profile</c>. The fields are the ones of
/// <c>POST /v0/itinerary</c>, with the same names, values and rules, so a saved profile can be
/// sent as is to compute an itinerary. Fields that do not apply to the chosen
/// <c>mobility_profile</c> are ignored and not stored.
/// </summary>
public class UserProfileRequest
{
    /// <summary>Consent to store the mobility profile, which is health data (GDPR). Must be <c>true</c>, otherwise the request is refused with a 400. Withdraw it with <c>DELETE /v0/users/me/profile</c>.</summary>
    /// <example>true</example>
    [Required]
    [MustBeTrue]
    [JsonPropertyName("consent")]
    public bool? Consent { get; set; }

    /// <summary>Free value stored for the apps and never used by the API, to restore the choice made in the form when several app choices share the same <c>mobility_profile</c> (for example stroller and pedestrian are both 0).</summary>
    /// <example>stroller</example>
    [StringLength(40)]
    [JsonPropertyName("app_profile")]
    public string? AppProfile { get; set; }

    /// <summary>The user's mobility profile. Conditions the relevance of the other fields.</summary>
    /// <example>1</example>
    [Required]
    [EnumDataType(typeof(MobilityProfile))]
    [JsonPropertyName("mobility_profile")]
    public MobilityProfile? MobilityProfile { get; set; }

    /// <summary>Id of the chosen model in <c>GET /v0/wheelchairs</c>. Wheelchair profile only. Optional: a user can enter a width without choosing a model. When it is set and <c>wheelchair_width</c> is not, the model's <c>recommended_wheelchair_width</c> is stored.</summary>
    /// <example>tilite-active</example>
    [StringLength(100)]
    [JsonPropertyName("wheelchair_model_id")]
    public string? WheelchairModelId { get; set; }

    /// <summary>Wheelchair width in meters. Wheelchair profile only.</summary>
    /// <example>0.7</example>
    [Range(0.0, 2.0, MinimumIsExclusive = true)]
    [JsonPropertyName("wheelchair_width")]
    public double? WheelchairWidth { get; set; }

    /// <summary>Physical strength of a wheelchair user. Wheelchair profile only.</summary>
    [EnumDataType(typeof(PhysicalStrength))]
    [JsonPropertyName("physical_strength")]
    public PhysicalStrength? PhysicalStrength { get; set; }

    /// <summary>Whether the user is accompanied. Wheelchair and Blind profiles only.</summary>
    [JsonPropertyName("is_accompanied")]
    public bool? IsAccompanied { get; set; }

    /// <summary>Whether a person on crutches can climb stairs. Crutches profile only.</summary>
    [JsonPropertyName("can_climb_stairs")]
    public bool? CanClimbStairs { get; set; }

    /// <summary>Route preferences, bitmask. Pedestrian profile only.</summary>
    /// <example>24</example>
    [EnumDataType(typeof(PathPreference))]
    [JsonPropertyName("path_preferences")]
    public PathPreference? PathPreferences { get; set; }
}

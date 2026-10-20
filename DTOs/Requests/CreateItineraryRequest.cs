using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proto_back.DTOs.Requests;

/// <summary>The user's mobility profile. Determines which optional fields on <c>CreateItineraryRequest</c> are relevant.</summary>
public enum MobilityProfile
{
    /// <summary>A pedestrian with no mobility impairment.</summary>
    Pedestrian = 0,

    /// <summary>A wheelchair user.</summary>
    Wheelchair = 1,

    /// <summary>A person walking with crutches.</summary>
    Crutches = 2,

    /// <summary>A blind or visually impaired person.</summary>
    Blind = 3
}

/// <summary>Physical strength level, used for wheelchair users. Determines tolerance to slopes and slightly raised curbs.</summary>
public enum PhysicalStrength
{
    /// <summary>Avoid every slope and every raised curb.</summary>
    Low = 0,

    /// <summary>Gentle slopes are acceptable.</summary>
    Medium = 1,

    /// <summary>Gentle slopes and slightly raised curbs are both acceptable.</summary>
    High = 2
}

/// <summary>Route preferences for able-bodied pedestrians. Combinable bit flags, e.g. <c>LitStreets | Benches</c> = 24.</summary>
[Flags]
public enum PathPreference
{
    /// <summary>No preference.</summary>
    None = 0,

    /// <summary>Prefer stairs over slopes.</summary>
    Stairs = 1 << 0,

    /// <summary>Prefer slopes over stairs.</summary>
    Slopes = 1 << 1,

    /// <summary>Prefer wide pathways.</summary>
    WidePathways = 1 << 2,

    /// <summary>Prioritize lit streets.</summary>
    LitStreets = 1 << 3,

    /// <summary>Require benches along the route (age, fatigue).</summary>
    Benches = 1 << 4
}

/// <summary>Request body for <c>POST /v0/itinerary</c>.</summary>
public class CreateItineraryRequest
{
    /// <summary>The starting point: either coordinates as <c>"lat,lng"</c> with <c>.</c> as the decimal separator, or a free-text address geocoded through Nominatim.</summary>
    /// <example>48.8566,2.3522</example>
    [Required]
    [JsonPropertyName("start")]
    public string Start { get; set; } = null!;

    /// <summary>The destination point, in the same formats as <c>start</c>.</summary>
    /// <example>48.8606,2.3376</example>
    [Required]
    [JsonPropertyName("end")]
    public string End { get; set; } = null!;

    // -------------------------------------------------------------------------
    // Mobility profile
    // -------------------------------------------------------------------------

    /// <summary>The user's mobility profile. Conditions the relevance of the other fields.</summary>
    /// <example>0</example>
    [Required]
    [JsonPropertyName("mobility_profile")]
    public MobilityProfile MobilityProfile { get; set; } = MobilityProfile.Pedestrian;

    // -------------------------------------------------------------------------
    // Wheelchair — specific fields
    // -------------------------------------------------------------------------

    /// <summary>Wheelchair width in meters. Applies to the Wheelchair profile. The returned path is guaranteed to be wider than this value.</summary>
    /// <example>0.7</example>
    [Range(0.0, 2.0, MinimumIsExclusive = true)]
    [JsonPropertyName("wheelchair_width")]
    public double? WheelchairWidth { get; set; }

    /// <summary>Physical strength of a wheelchair user. Applies to the Wheelchair profile. Determines tolerance to slopes and slightly raised curbs.</summary>
    [JsonPropertyName("physical_strength")]
    public PhysicalStrength? PhysicalStrength { get; set; }

    // -------------------------------------------------------------------------
    // Shared field: Wheelchair + Blind
    // -------------------------------------------------------------------------

    /// <summary>Whether the user is accompanied. Applies to the Wheelchair and Blind profiles. Wheelchair: allows gentle slopes even without physical strength. Blind: if not accompanied, a tactile paving path becomes mandatory.</summary>
    [JsonPropertyName("is_accompanied")]
    public bool? IsAccompanied { get; set; }

    // -------------------------------------------------------------------------
    // Crutches — specific fields
    // -------------------------------------------------------------------------

    /// <summary>Whether a person on crutches can climb stairs. Applies to the Crutches profile. true: stairs are acceptable (but not too many/too high). false: no stairs, no raised curbs.</summary>
    [JsonPropertyName("can_climb_stairs")]
    public bool? CanClimbStairs { get; set; }

    // -------------------------------------------------------------------------
    // Pedestrian — specific fields
    // -------------------------------------------------------------------------

    /// <summary>Route preferences for an able-bodied pedestrian. Applies to the Pedestrian profile. Example: <c>LitStreets | Benches</c> = 24.</summary>
    /// <example>24</example>
    [JsonPropertyName("path_preferences")]
    public PathPreference? PathPreferences { get; set; }
}

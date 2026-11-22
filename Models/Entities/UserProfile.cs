using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using proto_back.DTOs.Requests;

namespace proto_back.Models.Entities;

/// <summary>
/// Mobility profile saved by a signed-in user, stored in the <c>user_profiles</c> collection.
/// One profile per account (unique index on <c>userId</c>). The fields are the ones of
/// <see cref="CreateItineraryRequest"/>, so a saved profile can be turned into an itinerary
/// request as is. The profile can reveal a disability, which makes it health data (GDPR):
/// it is only saved with the user's consent, and deleted with the account.
/// </summary>
public class UserProfile
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    /// <summary>Id of the account the profile belongs to.</summary>
    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = null!;

    /// <summary>Free value stored for the apps and never used by the API.</summary>
    [BsonElement("appProfile")]
    [BsonIgnoreIfNull]
    public string? AppProfile { get; set; }

    [BsonElement("mobilityProfile")]
    public MobilityProfile MobilityProfile { get; set; }

    /// <summary>Id of the chosen model in the wheelchair catalog. Wheelchair profile only.</summary>
    [BsonElement("wheelchairModelId")]
    [BsonIgnoreIfNull]
    public string? WheelchairModelId { get; set; }

    /// <summary>Wheelchair width in meters. Wheelchair profile only.</summary>
    [BsonElement("wheelchairWidth")]
    [BsonIgnoreIfNull]
    public double? WheelchairWidth { get; set; }

    [BsonElement("physicalStrength")]
    [BsonIgnoreIfNull]
    public PhysicalStrength? PhysicalStrength { get; set; }

    [BsonElement("isAccompanied")]
    [BsonIgnoreIfNull]
    public bool? IsAccompanied { get; set; }

    [BsonElement("canClimbStairs")]
    [BsonIgnoreIfNull]
    public bool? CanClimbStairs { get; set; }

    [BsonElement("pathPreferences")]
    [BsonIgnoreIfNull]
    public PathPreference? PathPreferences { get; set; }

    /// <summary>When the user first gave the consent to store the profile. Kept when the profile is replaced.</summary>
    [BsonElement("consentGivenAt")]
    public DateTime ConsentGivenAt { get; set; }

    /// <summary>When the profile was first saved. Kept when the profile is replaced.</summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

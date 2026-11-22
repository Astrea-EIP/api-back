using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace proto_back.Models.Entities;

/// <summary>
/// A user account, stored in the <c>users</c> collection. The email address is unique
/// (unique index on <c>email</c>) and stored in lower case. The password is never stored,
/// only its hash. The account holds no health data: the mobility profile is a separate
/// <see cref="UserProfile"/>.
/// </summary>
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    /// <summary>Email address used to sign in, trimmed and in lower case.</summary>
    [BsonElement("email")]
    public string Email { get; set; } = null!;

    /// <summary>Date of birth, stored as midnight UTC.</summary>
    [BsonElement("birthDate")]
    public DateTime BirthDate { get; set; }

    /// <summary>Hash of the password (ASP.NET Core Identity format: PBKDF2 with a random salt).</summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = null!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}

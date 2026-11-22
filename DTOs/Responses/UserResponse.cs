using System.Text.Json.Serialization;

namespace proto_back.DTOs.Responses;

/// <summary>Response body for <c>GET /v0/users/me</c>: the signed-in user's account.</summary>
public class UserResponse
{
    /// <summary>Stable identifier of the account.</summary>
    /// <example>6740f1c2a9e3b54d2c1f0a77</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <example>Camille</example>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <example>camille@example.com</example>
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    /// <example>1998-04-12</example>
    [JsonPropertyName("birth_date")]
    public DateOnly BirthDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

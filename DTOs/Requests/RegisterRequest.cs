using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using proto_back.Shared.Validation;

namespace proto_back.DTOs.Requests;

/// <summary>Request body for <c>POST /v0/auth/register</c>. No consent is asked here: the account holds no health data.</summary>
public class RegisterRequest
{
    /// <summary>Name shown in the apps.</summary>
    /// <example>Camille</example>
    [Required]
    [StringLength(80, MinimumLength = 1)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>Email address used to sign in. Compared without case.</summary>
    /// <example>camille@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(254)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    /// <summary>At least 8 characters, with at least one letter and one digit (same rule as the web and mobile apps). Stored as a hash, never in clear text.</summary>
    /// <example>correct-horse-battery-9</example>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    [PasswordStrength]
    [JsonPropertyName("password")]
    public string Password { get; set; } = null!;

    /// <summary>Date of birth, ISO format <c>YYYY-MM-DD</c>. Must be in the past.</summary>
    /// <example>1998-04-12</example>
    [Required]
    [PastDate]
    [JsonPropertyName("birth_date")]
    public DateOnly? BirthDate { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proto_back.DTOs.Requests;

/// <summary>Request body for <c>POST /v0/auth/login</c>.</summary>
public class LoginRequest
{
    /// <summary>Email address of the account. Compared without case.</summary>
    /// <example>camille@example.com</example>
    [Required]
    [JsonPropertyName("email")]
    public string Email { get; set; } = null!;

    /// <summary>Password of the account.</summary>
    /// <example>correct-horse-battery-9</example>
    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace proto_back.Shared.Validation;

/// <summary>
/// Password rule shared with the web and mobile apps: at least one letter (any alphabet,
/// accents included) and one digit from 0 to 9. The length is checked by
/// <see cref="StringLengthAttribute"/>. A missing value is left to <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PasswordStrengthAttribute : ValidationAttribute
{
    public PasswordStrengthAttribute()
        : base("The {0} field must contain at least one letter and one digit.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string password
        || (password.Any(char.IsLetter) && password.Any(c => c is >= '0' and <= '9'));
}

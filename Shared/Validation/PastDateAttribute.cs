using System.ComponentModel.DataAnnotations;

namespace proto_back.Shared.Validation;

/// <summary>
/// Accepts a <see cref="DateOnly"/> strictly before today (UTC). A missing value is left to
/// <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PastDateAttribute : ValidationAttribute
{
    public PastDateAttribute()
        : base("The {0} field must be a date in the past.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not DateOnly date || date < DateOnly.FromDateTime(DateTime.UtcNow);
}

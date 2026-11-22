using System.ComponentModel.DataAnnotations;

namespace proto_back.Shared.Validation;

/// <summary>
/// Accepts only <c>true</c>. Used for a consent that must be given explicitly: a missing value
/// or <c>false</c> is refused.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public MustBeTrueAttribute()
        : base("The {0} field must be true.")
    {
    }

    public override bool IsValid(object? value) => value is true;
}

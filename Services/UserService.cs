using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IRepositories;
using proto_back.Interfaces.IServices;
using proto_back.Models.Entities;
using proto_back.Shared.Errors;

namespace proto_back.Services;

/// <summary>
/// Reads, saves and deletes the signed-in user's account and mobility profile.
/// A profile only keeps the fields that apply to its <c>mobility_profile</c>, with the same
/// rules as <c>POST /v0/itinerary</c>.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUserProfileRepository _profiles;
    private readonly IWheelchairService _wheelchairs;
    private readonly TimeProvider _timeProvider;

    public UserService(
        IUserRepository users,
        IUserProfileRepository profiles,
        IWheelchairService wheelchairs,
        TimeProvider? timeProvider = null)
    {
        _users = users;
        _profiles = profiles;
        _wheelchairs = wheelchairs;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<bool> AccountExistsAsync(string userId) =>
        await _users.GetByIdAsync(userId) is not null;

    public async Task<UserResponse?> GetAccountAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);
        return user is null
            ? null
            : new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                BirthDate = DateOnly.FromDateTime(user.BirthDate),
                CreatedAt = user.CreatedAt
            };
    }

    public async Task<bool> DeleteAccountAsync(string userId)
    {
        if (!await AccountExistsAsync(userId))
        {
            return false;
        }

        // The profile first: if the second step fails, no health data is left behind.
        await _profiles.DeleteByUserIdAsync(userId);
        await _users.DeleteAsync(userId);
        return true;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(string userId)
    {
        var profile = await _profiles.GetByUserIdAsync(userId);
        return profile is null ? null : ToResponse(profile);
    }

    public async Task<UserProfileResponse> SaveProfileAsync(string userId, UserProfileRequest request)
    {
        var mobilityProfile = request.MobilityProfile!.Value;
        var isWheelchair = mobilityProfile == MobilityProfile.Wheelchair;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var profile = new UserProfile
        {
            UserId = userId,
            AppProfile = string.IsNullOrWhiteSpace(request.AppProfile) ? null : request.AppProfile.Trim(),
            MobilityProfile = mobilityProfile,
            WheelchairWidth = isWheelchair ? request.WheelchairWidth : null,
            PhysicalStrength = isWheelchair ? request.PhysicalStrength : null,
            IsAccompanied = mobilityProfile is MobilityProfile.Wheelchair or MobilityProfile.Blind ? request.IsAccompanied : null,
            CanClimbStairs = mobilityProfile == MobilityProfile.Crutches ? request.CanClimbStairs : null,
            PathPreferences = mobilityProfile == MobilityProfile.Pedestrian ? request.PathPreferences : null,
            ConsentGivenAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (isWheelchair && !string.IsNullOrWhiteSpace(request.WheelchairModelId))
        {
            var modelId = request.WheelchairModelId.Trim();
            var model = _wheelchairs.GetCatalog().Categories
                .SelectMany(c => c.Models)
                .FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.OrdinalIgnoreCase))
                ?? throw new UnknownWheelchairModelException(modelId);

            profile.WheelchairModelId = model.Id;
            profile.WheelchairWidth ??= model.RecommendedWheelchairWidth;
        }

        return ToResponse(await _profiles.SaveAsync(profile));
    }

    public Task DeleteProfileAsync(string userId) => _profiles.DeleteByUserIdAsync(userId);

    private static UserProfileResponse ToResponse(UserProfile profile) => new()
    {
        Consent = true,
        AppProfile = profile.AppProfile,
        MobilityProfile = profile.MobilityProfile,
        WheelchairModelId = profile.WheelchairModelId,
        WheelchairWidth = profile.WheelchairWidth,
        PhysicalStrength = profile.PhysicalStrength,
        IsAccompanied = profile.IsAccompanied,
        CanClimbStairs = profile.CanClimbStairs,
        PathPreferences = profile.PathPreferences,
        ConsentGivenAt = profile.ConsentGivenAt,
        UpdatedAt = profile.UpdatedAt
    };
}

using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;

namespace proto_back.Interfaces.IServices;

/// <summary>
/// The signed-in user's account and mobility profile. Every method takes the user id from the
/// token; tokens are not revoked, so the account may have been deleted since the token was issued.
/// </summary>
public interface IUserService
{
    /// <summary>Whether the account still exists.</summary>
    Task<bool> AccountExistsAsync(string userId);

    /// <summary>Returns the account, or <c>null</c> if it no longer exists.</summary>
    Task<UserResponse?> GetAccountAsync(string userId);

    /// <summary>Deletes the account and its profile. Returns <c>false</c> if the account no longer exists.</summary>
    Task<bool> DeleteAccountAsync(string userId);

    /// <summary>Returns the saved profile, or <c>null</c> if none was saved.</summary>
    Task<UserProfileResponse?> GetProfileAsync(string userId);

    /// <summary>
    /// Creates or replaces the profile. Throws <see cref="Shared.Errors.UnknownWheelchairModelException"/>
    /// when <c>wheelchair_model_id</c> is not in the catalog.
    /// </summary>
    Task<UserProfileResponse> SaveProfileAsync(string userId, UserProfileRequest request);

    /// <summary>Deletes the profile and the consent date (withdrawal of the consent). Does nothing if there is none.</summary>
    Task DeleteProfileAsync(string userId);
}

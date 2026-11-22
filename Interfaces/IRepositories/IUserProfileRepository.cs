using proto_back.Models.Entities;

namespace proto_back.Interfaces.IRepositories;

public interface IUserProfileRepository
{
    /// <summary>Returns the profile of the account, or <c>null</c> if none was saved.</summary>
    Task<UserProfile?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Creates or replaces the profile of <see cref="UserProfile.UserId"/> and returns the
    /// stored document. On a replacement, <see cref="UserProfile.CreatedAt"/> and
    /// <see cref="UserProfile.ConsentGivenAt"/> keep their first values; the optional fields
    /// left <c>null</c> are removed from the stored document.
    /// </summary>
    Task<UserProfile> SaveAsync(UserProfile profile);

    /// <summary>Deletes the profile of the account. Returns <c>false</c> if there was none.</summary>
    Task<bool> DeleteByUserIdAsync(string userId);
}

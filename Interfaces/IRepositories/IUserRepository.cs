using proto_back.Models.Entities;

namespace proto_back.Interfaces.IRepositories;

public interface IUserRepository
{
    /// <summary>
    /// Inserts the account and sets its <see cref="User.Id"/>. Returns <c>false</c>, without
    /// inserting anything, when an account already uses the same email address.
    /// </summary>
    Task<bool> TryCreateAsync(User user);

    /// <summary>Returns the account with this email address (already in lower case), or <c>null</c>.</summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>Returns the account with this id, or <c>null</c> (also for an id that is not an ObjectId).</summary>
    Task<User?> GetByIdAsync(string id);

    /// <summary>Deletes the account. Returns <c>false</c> if there was none.</summary>
    Task<bool> DeleteAsync(string id);
}

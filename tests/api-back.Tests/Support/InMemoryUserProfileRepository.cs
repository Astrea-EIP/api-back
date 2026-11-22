using System.Collections.Concurrent;
using MongoDB.Bson;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Tests.Support;

/// <summary>
/// In-memory <see cref="IUserProfileRepository"/> used to replace the real MongoDB-backed
/// repository in tests, with the same rules: one profile per account, and the first
/// <c>createdAt</c> and <c>consentGivenAt</c> kept when the profile is replaced.
/// </summary>
public class InMemoryUserProfileRepository : IUserProfileRepository
{
    private readonly ConcurrentDictionary<string, UserProfile> _profilesByUserId = new();

    public IReadOnlyCollection<UserProfile> Profiles => _profilesByUserId.Values.ToArray();

    public Task<UserProfile?> GetByUserIdAsync(string userId)
    {
        return Task.FromResult(_profilesByUserId.TryGetValue(userId, out var profile) ? profile : null);
    }

    public Task<UserProfile> SaveAsync(UserProfile profile)
    {
        var saved = _profilesByUserId.AddOrUpdate(
            profile.UserId,
            _ =>
            {
                profile.Id ??= ObjectId.GenerateNewId().ToString();
                return profile;
            },
            (_, existing) =>
            {
                profile.Id = existing.Id;
                profile.CreatedAt = existing.CreatedAt;
                profile.ConsentGivenAt = existing.ConsentGivenAt;
                return profile;
            });
        return Task.FromResult(saved);
    }

    public Task<bool> DeleteByUserIdAsync(string userId)
    {
        return Task.FromResult(_profilesByUserId.TryRemove(userId, out _));
    }
}

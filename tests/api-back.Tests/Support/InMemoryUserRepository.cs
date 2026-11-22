using System.Collections.Concurrent;
using MongoDB.Bson;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Tests.Support;

/// <summary>
/// In-memory <see cref="IUserRepository"/> used to replace the real MongoDB-backed repository
/// in end-to-end tests, with the same unique email rule.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _usersByEmail = new();

    public IReadOnlyCollection<User> Users => _usersByEmail.Values.ToArray();

    public Task<bool> TryCreateAsync(User user)
    {
        user.Id ??= ObjectId.GenerateNewId().ToString();
        return Task.FromResult(_usersByEmail.TryAdd(user.Email, user));
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return Task.FromResult(_usersByEmail.TryGetValue(email, out var user) ? user : null);
    }

    public Task<User?> GetByIdAsync(string id)
    {
        return Task.FromResult(_usersByEmail.Values.FirstOrDefault(u => u.Id == id));
    }

    public Task<bool> DeleteAsync(string id)
    {
        var user = _usersByEmail.Values.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user is not null && _usersByEmail.TryRemove(user.Email, out _));
    }
}

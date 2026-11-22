using MongoDB.Bson;
using MongoDB.Driver;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Repositories;

public class UserRepository : IUserRepository
{
    public const string CollectionName = "users";

    private readonly IMongoCollection<User> _collection;

    public UserRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<User>(CollectionName);
    }

    public async Task<bool> TryCreateAsync(User user)
    {
        await EnsureIndexesAsync();

        try
        {
            await _collection.InsertOneAsync(user);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        await EnsureIndexesAsync();
        return await _collection.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        await EnsureIndexesAsync();
        return await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        await EnsureIndexesAsync();
        var result = await _collection.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    private Task EnsureIndexesAsync() => MongoIndexes.EnsureAsync(
        _collection,
        new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "email_unique" }));
}

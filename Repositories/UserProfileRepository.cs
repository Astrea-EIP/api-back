using System.Linq.Expressions;
using MongoDB.Driver;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    public const string CollectionName = "user_profiles";

    private readonly IMongoCollection<UserProfile> _collection;

    public UserProfileRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UserProfile>(CollectionName);
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId)
    {
        await EnsureIndexesAsync();
        return await _collection.Find(p => p.UserId == userId).FirstOrDefaultAsync();
    }

    public async Task<UserProfile> SaveAsync(UserProfile profile)
    {
        await EnsureIndexesAsync();

        var update = Builders<UserProfile>.Update;
        var changes = new List<UpdateDefinition<UserProfile>>
        {
            update.SetOnInsert(p => p.UserId, profile.UserId),
            update.SetOnInsert(p => p.ConsentGivenAt, profile.ConsentGivenAt),
            update.SetOnInsert(p => p.CreatedAt, profile.CreatedAt),
            update.Set(p => p.MobilityProfile, profile.MobilityProfile),
            update.Set(p => p.UpdatedAt, profile.UpdatedAt),
            SetOrUnset(p => p.AppProfile, profile.AppProfile),
            SetOrUnset(p => p.WheelchairModelId, profile.WheelchairModelId),
            SetOrUnset(p => p.WheelchairWidth, profile.WheelchairWidth),
            SetOrUnset(p => p.PhysicalStrength, profile.PhysicalStrength),
            SetOrUnset(p => p.IsAccompanied, profile.IsAccompanied),
            SetOrUnset(p => p.CanClimbStairs, profile.CanClimbStairs),
            SetOrUnset(p => p.PathPreferences, profile.PathPreferences)
        };

        var options = new FindOneAndUpdateOptions<UserProfile>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        try
        {
            return await _collection.FindOneAndUpdateAsync<UserProfile>(
                p => p.UserId == profile.UserId, update.Combine(changes), options);
        }
        catch (MongoCommandException ex) when (ex.Code == 11000)
        {
            // Two first saves raced: the other one inserted the document, so this one updates it.
            return await _collection.FindOneAndUpdateAsync<UserProfile>(
                p => p.UserId == profile.UserId, update.Combine(changes), options);
        }
    }

    public async Task<bool> DeleteByUserIdAsync(string userId)
    {
        await EnsureIndexesAsync();
        var result = await _collection.DeleteOneAsync(p => p.UserId == userId);
        return result.DeletedCount > 0;
    }

    // An optional field left null is removed from the document instead of being stored as null.
    private static UpdateDefinition<UserProfile> SetOrUnset<TField>(
        Expression<Func<UserProfile, TField>> field, TField value)
    {
        return value is null
            ? Builders<UserProfile>.Update.Unset(new ExpressionFieldDefinition<UserProfile>(field))
            : Builders<UserProfile>.Update.Set(field, value);
    }

    private Task EnsureIndexesAsync() => MongoIndexes.EnsureAsync(
        _collection,
        new CreateIndexModel<UserProfile>(
            Builders<UserProfile>.IndexKeys.Ascending(p => p.UserId),
            new CreateIndexOptions { Unique = true, Name = "userId_unique" }));
}

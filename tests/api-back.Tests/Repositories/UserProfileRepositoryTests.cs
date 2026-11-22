using MongoDB.Bson;
using MongoDB.Driver;
using proto_back.DTOs.Requests;
using proto_back.Models.Entities;
using proto_back.Repositories;
using proto_back.Tests.Support;

namespace proto_back.Tests.Repositories;

[Trait("Category", "Database")]
public class UserProfileRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly IMongoDatabase _database;
    private readonly UserProfileRepository _repository;

    public UserProfileRepositoryTests(MongoDbFixture fixture)
    {
        var client = new MongoClient(fixture.ConnectionString);
        _database = client.GetDatabase("api_back_tests");
        _repository = new UserProfileRepository(_database);
    }

    private static DateTime Utc(int minute) => new(2026, 11, 21, 10, minute, 0, DateTimeKind.Utc);

    private static UserProfile NewWheelchairProfile(string? userId = null) => new()
    {
        UserId = userId ?? ObjectId.GenerateNewId().ToString(),
        AppProfile = "manual-wheelchair",
        MobilityProfile = MobilityProfile.Wheelchair,
        WheelchairModelId = "tilite-active",
        WheelchairWidth = 0.7,
        PhysicalStrength = PhysicalStrength.Medium,
        IsAccompanied = true,
        ConsentGivenAt = Utc(0),
        CreatedAt = Utc(0),
        UpdatedAt = Utc(0)
    };

    private Task<BsonDocument?> GetRawAsync(string userId) =>
        _database.GetCollection<BsonDocument>(UserProfileRepository.CollectionName)
            .Find(new BsonDocument("userId", ObjectId.Parse(userId)))
            .FirstOrDefaultAsync()!;

    [Fact]
    public async Task SaveAsync_NewProfile_ThenGetByUserIdAsync_ReturnsEquivalentProfileWithIdAssigned()
    {
        var profile = NewWheelchairProfile();

        var saved = await _repository.SaveAsync(profile);
        var fetched = await _repository.GetByUserIdAsync(profile.UserId);

        Assert.False(string.IsNullOrEmpty(saved.Id));
        Assert.NotNull(fetched);
        Assert.Equal(saved.Id, fetched!.Id);
        Assert.Equal(profile.UserId, fetched.UserId);
        Assert.Equal("manual-wheelchair", fetched.AppProfile);
        Assert.Equal(MobilityProfile.Wheelchair, fetched.MobilityProfile);
        Assert.Equal("tilite-active", fetched.WheelchairModelId);
        Assert.Equal(0.7, fetched.WheelchairWidth);
        Assert.Equal(PhysicalStrength.Medium, fetched.PhysicalStrength);
        Assert.True(fetched.IsAccompanied);
        Assert.Null(fetched.CanClimbStairs);
        Assert.Null(fetched.PathPreferences);
        Assert.Equal(Utc(0), fetched.ConsentGivenAt);
        Assert.Equal(Utc(0), fetched.CreatedAt);
        Assert.Equal(Utc(0), fetched.UpdatedAt);
    }

    [Fact]
    public async Task SaveAsync_ExistingProfile_ReplacesFieldsAndKeepsIdCreationAndConsentDates()
    {
        var first = await _repository.SaveAsync(NewWheelchairProfile());

        var replacement = new UserProfile
        {
            UserId = first.UserId,
            MobilityProfile = MobilityProfile.Pedestrian,
            PathPreferences = PathPreference.LitStreets | PathPreference.Benches,
            ConsentGivenAt = Utc(5),
            CreatedAt = Utc(5),
            UpdatedAt = Utc(5)
        };
        var saved = await _repository.SaveAsync(replacement);

        Assert.Equal(first.Id, saved.Id);
        Assert.Equal(MobilityProfile.Pedestrian, saved.MobilityProfile);
        Assert.Equal(PathPreference.LitStreets | PathPreference.Benches, saved.PathPreferences);
        Assert.Null(saved.AppProfile);
        Assert.Null(saved.WheelchairModelId);
        Assert.Null(saved.WheelchairWidth);
        Assert.Null(saved.PhysicalStrength);
        Assert.Null(saved.IsAccompanied);
        Assert.Equal(Utc(0), saved.ConsentGivenAt);
        Assert.Equal(Utc(0), saved.CreatedAt);
        Assert.Equal(Utc(5), saved.UpdatedAt);
    }

    [Fact]
    public async Task SaveAsync_FieldsLeftNull_AreNotStored()
    {
        var replacement = new UserProfile
        {
            UserId = (await _repository.SaveAsync(NewWheelchairProfile())).UserId,
            MobilityProfile = MobilityProfile.Crutches,
            CanClimbStairs = false,
            ConsentGivenAt = Utc(1),
            CreatedAt = Utc(1),
            UpdatedAt = Utc(1)
        };
        await _repository.SaveAsync(replacement);

        var raw = await GetRawAsync(replacement.UserId);

        Assert.NotNull(raw);
        var fields = raw!.Names.ToHashSet();
        Assert.Equal(
            new HashSet<string> { "_id", "userId", "mobilityProfile", "canClimbStairs", "consentGivenAt", "createdAt", "updatedAt" },
            fields);
    }

    [Fact]
    public async Task SaveAsync_StoredDocument_UsesDocumentedFieldNamesAndObjectIdUserId()
    {
        var profile = NewWheelchairProfile();
        profile.CanClimbStairs = true;
        profile.PathPreferences = PathPreference.Stairs;
        await _repository.SaveAsync(profile);

        var raw = await GetRawAsync(profile.UserId);

        Assert.NotNull(raw);
        foreach (var field in new[]
                 {
                     "userId", "appProfile", "mobilityProfile", "wheelchairModelId", "wheelchairWidth",
                     "physicalStrength", "isAccompanied", "canClimbStairs", "pathPreferences",
                     "consentGivenAt", "createdAt", "updatedAt"
                 })
        {
            Assert.True(raw!.Contains(field), $"Expected field '{field}' in stored document.");
        }

        Assert.Equal(BsonType.ObjectId, raw!["userId"].BsonType);
        Assert.Equal(1, raw["mobilityProfile"].AsInt32);
    }

    [Fact]
    public async Task SaveAsync_CreatesAUniqueIndexOnUserId()
    {
        await _repository.SaveAsync(NewWheelchairProfile());

        var indexes = await (await _database.GetCollection<BsonDocument>(UserProfileRepository.CollectionName)
            .Indexes.ListAsync()).ToListAsync();

        var userIdIndex = Assert.Single(indexes, i => i["key"].AsBsonDocument.Contains("userId"));
        Assert.True(userIdIndex.GetValue("unique", false).ToBoolean());
    }

    [Fact]
    public async Task SaveAsync_TwoProfilesForTheSameUser_KeepsASingleDocument()
    {
        var userId = ObjectId.GenerateNewId().ToString();

        await Task.WhenAll(
            _repository.SaveAsync(NewWheelchairProfile(userId)),
            _repository.SaveAsync(NewWheelchairProfile(userId)));

        var count = await _database.GetCollection<BsonDocument>(UserProfileRepository.CollectionName)
            .CountDocumentsAsync(new BsonDocument("userId", ObjectId.Parse(userId)));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByUserIdAsync_NoProfile_ReturnsNull()
    {
        var fetched = await _repository.GetByUserIdAsync(ObjectId.GenerateNewId().ToString());

        Assert.Null(fetched);
    }

    [Fact]
    public async Task DeleteByUserIdAsync_ExistingProfile_DeletesItAndReturnsTrue()
    {
        var profile = await _repository.SaveAsync(NewWheelchairProfile());

        var deleted = await _repository.DeleteByUserIdAsync(profile.UserId);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByUserIdAsync(profile.UserId));
    }

    [Fact]
    public async Task DeleteByUserIdAsync_NoProfile_ReturnsFalse()
    {
        var deleted = await _repository.DeleteByUserIdAsync(ObjectId.GenerateNewId().ToString());

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteByUserIdAsync_OnlyDeletesTheProfileOfThatUser()
    {
        var kept = await _repository.SaveAsync(NewWheelchairProfile());
        var removed = await _repository.SaveAsync(NewWheelchairProfile());

        await _repository.DeleteByUserIdAsync(removed.UserId);

        Assert.NotNull(await _repository.GetByUserIdAsync(kept.UserId));
    }
}

using MongoDB.Bson;
using MongoDB.Driver;
using proto_back.Models.Entities;
using proto_back.Repositories;
using proto_back.Tests.Support;

namespace proto_back.Tests.Repositories;

[Trait("Category", "Database")]
public class UserRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly IMongoDatabase _database;
    private readonly UserRepository _repository;

    public UserRepositoryTests(MongoDbFixture fixture)
    {
        var client = new MongoClient(fixture.ConnectionString);
        _database = client.GetDatabase("api_back_tests");
        _repository = new UserRepository(_database);
    }

    private static User NewUser(string? email = null) => new()
    {
        Name = "Camille",
        Email = email ?? $"{Guid.NewGuid():N}@example.com",
        BirthDate = new DateTime(1998, 4, 12, 0, 0, 0, DateTimeKind.Utc),
        PasswordHash = "hash",
        CreatedAt = new DateTime(2026, 11, 22, 14, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task TryCreateAsync_NewEmail_ReturnsTrueAndAssignsAnId()
    {
        var user = NewUser();

        var created = await _repository.TryCreateAsync(user);

        Assert.True(created);
        Assert.True(ObjectId.TryParse(user.Id, out _));
    }

    [Fact]
    public async Task TryCreateAsync_ThenGetByEmailAsync_ReturnsEquivalentAccount()
    {
        var user = NewUser();
        await _repository.TryCreateAsync(user);

        var fetched = await _repository.GetByEmailAsync(user.Email);

        Assert.NotNull(fetched);
        Assert.Equal(user.Id, fetched!.Id);
        Assert.Equal(user.Name, fetched.Name);
        Assert.Equal(user.Email, fetched.Email);
        Assert.Equal(user.BirthDate, fetched.BirthDate);
        Assert.Equal(user.PasswordHash, fetched.PasswordHash);
        Assert.Equal(user.CreatedAt, fetched.CreatedAt);
    }

    [Fact]
    public async Task TryCreateAsync_EmailAlreadyUsed_ReturnsFalseAndKeepsTheFirstAccount()
    {
        var first = NewUser();
        await _repository.TryCreateAsync(first);
        var second = NewUser(first.Email);
        second.Name = "Someone else";

        var created = await _repository.TryCreateAsync(second);

        Assert.False(created);
        Assert.Equal("Camille", (await _repository.GetByEmailAsync(first.Email))!.Name);
        var count = await _database.GetCollection<BsonDocument>(UserRepository.CollectionName)
            .CountDocumentsAsync(new BsonDocument("email", first.Email));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task TryCreateAsync_CreatesAUniqueIndexOnEmail()
    {
        await _repository.TryCreateAsync(NewUser());

        var indexes = await (await _database.GetCollection<BsonDocument>(UserRepository.CollectionName)
            .Indexes.ListAsync()).ToListAsync();

        var emailIndex = Assert.Single(indexes, i => i["key"].AsBsonDocument.Contains("email"));
        Assert.True(emailIndex.GetValue("unique", false).ToBoolean());
    }

    [Fact]
    public async Task TryCreateAsync_StoredDocument_UsesDocumentedFieldNames()
    {
        var user = NewUser();
        await _repository.TryCreateAsync(user);

        var raw = await _database.GetCollection<BsonDocument>(UserRepository.CollectionName)
            .Find(new BsonDocument("email", user.Email))
            .FirstOrDefaultAsync();

        Assert.NotNull(raw);
        Assert.Equal(
            new HashSet<string> { "_id", "name", "email", "birthDate", "passwordHash", "createdAt" },
            raw!.Names.ToHashSet());
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        var fetched = await _repository.GetByEmailAsync($"{Guid.NewGuid():N}@example.com");

        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAccount_ReturnsIt()
    {
        var user = NewUser();
        await _repository.TryCreateAsync(user);

        var fetched = await _repository.GetByIdAsync(user.Id);

        Assert.NotNull(fetched);
        Assert.Equal(user.Email, fetched!.Email);
    }

    [Theory]
    [InlineData("6740f1c2a9e3b54d2c1f0a77")]
    [InlineData("not-an-object-id")]
    public async Task GetByIdAsync_UnknownOrInvalidId_ReturnsNull(string id)
    {
        Assert.Null(await _repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_ExistingAccount_DeletesOnlyItAndReturnsTrue()
    {
        var removed = NewUser();
        var kept = NewUser();
        await _repository.TryCreateAsync(removed);
        await _repository.TryCreateAsync(kept);

        var deleted = await _repository.DeleteAsync(removed.Id);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(removed.Id));
        Assert.NotNull(await _repository.GetByIdAsync(kept.Id));
    }

    [Theory]
    [InlineData("6740f1c2a9e3b54d2c1f0a77")]
    [InlineData("not-an-object-id")]
    public async Task DeleteAsync_UnknownOrInvalidId_ReturnsFalse(string id)
    {
        Assert.False(await _repository.DeleteAsync(id));
    }
}

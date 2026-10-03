using MongoDB.Bson;
using MongoDB.Driver;
using proto_back.Models.Entities;
using proto_back.Repositories;
using proto_back.Tests.Support;

namespace proto_back.Tests.Repositories;

[Trait("Category", "Database")]
public class ErrorLogRepositoryTests : IClassFixture<MongoDbFixture>
{
    private readonly IMongoDatabase _database;
    private readonly ErrorLogRepository _repository;

    public ErrorLogRepositoryTests(MongoDbFixture fixture)
    {
        var client = new MongoClient(fixture.ConnectionString);
        _database = client.GetDatabase("api_back_tests");
        _repository = new ErrorLogRepository(_database);
    }

    private static ErrorLog NewErrorLog(string? errorId = null) => new()
    {
        ErrorId = errorId ?? Guid.NewGuid().ToString("N"),
        Message = "boom",
        StackTrace = "   at Foo.Bar()",
        RequestPath = "/v0/itinerary",
        HttpMethod = "POST",
        OccurredAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_ThenGetByErrorIdAsync_ReturnsEquivalentDocumentWithIdAssigned()
    {
        var errorLog = NewErrorLog();

        await _repository.CreateAsync(errorLog);
        var fetched = await _repository.GetByErrorIdAsync(errorLog.ErrorId);

        Assert.NotNull(fetched);
        Assert.False(string.IsNullOrEmpty(fetched!.Id));
        Assert.Equal(errorLog.ErrorId, fetched.ErrorId);
        Assert.Equal(errorLog.Message, fetched.Message);
        Assert.Equal(errorLog.StackTrace, fetched.StackTrace);
        Assert.Equal(errorLog.RequestPath, fetched.RequestPath);
        Assert.Equal(errorLog.HttpMethod, fetched.HttpMethod);
    }

    [Fact]
    public async Task GetByErrorIdAsync_UnknownId_ReturnsNull()
    {
        var fetched = await _repository.GetByErrorIdAsync(Guid.NewGuid().ToString("N"));

        Assert.Null(fetched);
    }

    [Fact]
    public async Task CreateAsync_StoredDocument_UsesDocumentedFieldNames()
    {
        var errorLog = NewErrorLog();
        await _repository.CreateAsync(errorLog);

        var raw = await _database.GetCollection<BsonDocument>("error_logs")
            .Find(new BsonDocument("errorId", errorLog.ErrorId))
            .FirstOrDefaultAsync();

        Assert.NotNull(raw);
        foreach (var field in new[] { "errorId", "message", "stackTrace", "requestPath", "httpMethod", "occurredAt" })
        {
            Assert.True(raw!.Contains(field), $"Expected field '{field}' in stored document.");
        }
    }

    [Fact]
    public async Task CreateAsync_OccurredAt_RoundTripsAsUtcWithinOneMillisecond()
    {
        var errorLog = NewErrorLog();
        var original = errorLog.OccurredAt;

        await _repository.CreateAsync(errorLog);
        var fetched = await _repository.GetByErrorIdAsync(errorLog.ErrorId);

        Assert.NotNull(fetched);
        Assert.Equal(DateTimeKind.Utc, fetched!.OccurredAt.Kind);
        Assert.True((fetched.OccurredAt - original).Duration() < TimeSpan.FromMilliseconds(1));
    }
}

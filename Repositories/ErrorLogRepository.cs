using MongoDB.Driver;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Repositories;

public class ErrorLogRepository : IErrorLogRepository
{
    private readonly IMongoCollection<ErrorLog> _collection;

    public ErrorLogRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ErrorLog>("error_logs");
    }

    public async Task CreateAsync(ErrorLog errorLog)
    {
        await _collection.InsertOneAsync(errorLog);
    }

    public async Task<ErrorLog?> GetByErrorIdAsync(string errorId)
    {
        return await _collection
            .Find(e => e.ErrorId == errorId)
            .FirstOrDefaultAsync();
    }
}

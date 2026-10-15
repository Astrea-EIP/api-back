using System.Collections.Concurrent;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Tests.Support;

/// <summary>
/// In-memory <see cref="IErrorLogRepository"/> used to replace the real MongoDB-backed
/// repository in tests, so no real Mongo connection is ever needed outside the
/// Database-category tests.
/// </summary>
public class InMemoryErrorLogRepository : IErrorLogRepository
{
    private readonly ConcurrentBag<ErrorLog> _logs = new();

    public bool ThrowOnCreate { get; set; }

    public IReadOnlyCollection<ErrorLog> Logs => _logs.ToArray();

    public Task CreateAsync(ErrorLog errorLog)
    {
        if (ThrowOnCreate)
        {
            throw new InvalidOperationException("Simulated repository failure.");
        }

        errorLog.Id ??= Guid.NewGuid().ToString("N");
        _logs.Add(errorLog);
        return Task.CompletedTask;
    }

    public Task<ErrorLog?> GetByErrorIdAsync(string errorId)
    {
        return Task.FromResult(_logs.FirstOrDefault(l => l.ErrorId == errorId));
    }
}

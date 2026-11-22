using System.Collections.Concurrent;
using MongoDB.Driver;

namespace proto_back.Repositories;

/// <summary>
/// Creates the indexes a repository needs the first time the collection is used, once per
/// collection and per process. Creating them at startup would connect to MongoDB even when
/// it is not needed (for example in tests that replace the repositories). Creating an
/// index that already exists does nothing, so several instances of the API can share a
/// database. If the creation fails, the next call tries again.
/// </summary>
internal static class MongoIndexes
{
    private static readonly ConcurrentDictionary<string, Lazy<Task>> Created = new();

    public static async Task EnsureAsync<T>(IMongoCollection<T> collection, params CreateIndexModel<T>[] indexes)
    {
        var key = $"{collection.Database.Client.Settings.Server}/{collection.CollectionNamespace.FullName}";
        var creation = Created.GetOrAdd(key, _ => new Lazy<Task>(() => collection.Indexes.CreateManyAsync(indexes)));

        try
        {
            await creation.Value;
        }
        catch
        {
            Created.TryRemove(new KeyValuePair<string, Lazy<Task>>(key, creation));
            throw;
        }
    }
}

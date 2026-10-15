using Testcontainers.MongoDb;

namespace proto_back.Tests.Support;

/// <summary>
/// Starts a real, disposable MongoDB container via Testcontainers, shared by every test
/// in a class that implements <see cref="Xunit.IClassFixture{TFixture}"/> with this type
/// (one container per test class, per the Database test category). Requires Docker.
/// </summary>
public sealed class MongoDbFixture : IAsyncLifetime
{
    // Pin to the same major version as docker-compose.yml's `mongo` service.
    private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage("mongo:7").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

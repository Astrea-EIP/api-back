# Repositories

This folder contains **repository classes** that implement the data access layer using MongoDB.

## Conventions

- One repository per entity (e.g., `UserRepository.cs`, `ProductRepository.cs`).
- Each repository **must implement** its corresponding interface from `Interfaces/IRepositories/`.
- Repositories receive `IMongoDatabase` or `IMongoCollection<T>` via constructor injection.
- Keep repositories focused on CRUD operations — no business logic.

## Example

```csharp
using MongoDB.Driver;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;

namespace proto_back.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<User>("users");
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(User user)
    {
        await _collection.InsertOneAsync(user);
    }
}
```

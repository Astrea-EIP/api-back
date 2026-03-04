# Interfaces / IRepositories

This folder contains **repository interface contracts** that define the data access operations.

## Conventions

- One interface per entity (e.g., `IUserRepository.cs`, `IProductRepository.cs`).
- Prefix all interfaces with `I` (C# convention).
- Define only the method signatures — no implementation.
- These interfaces are registered in DI and enable unit testing with mocks (e.g., via `Moq`).

## Example

```csharp
using proto_back.Models.Entities;

namespace proto_back.Interfaces.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<IEnumerable<User>> GetAllAsync();
    Task CreateAsync(User user);
    Task UpdateAsync(string id, User user);
    Task DeleteAsync(string id);
}
```

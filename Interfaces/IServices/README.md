# Interfaces / IServices

This folder contains **service interface contracts** that define the business logic operations.

## Conventions

- One interface per service (e.g., `IUserService.cs`, `IAuthService.cs`).
- Prefix all interfaces with `I` (C# convention).
- Define only the method signatures — no implementation.
- These interfaces are registered in DI and enable unit testing with mocks.

## Example

```csharp
using proto_back.Models.Entities;

namespace proto_back.Interfaces.IServices;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task CreateUserAsync(User user);
}
```

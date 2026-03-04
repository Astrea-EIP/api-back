# Services

This folder contains **service classes** that implement the business logic layer.

## Conventions

- One service per domain area (e.g., `UserService.cs`, `AuthService.cs`).
- Each service **must implement** its corresponding interface from `Interfaces/IServices/`.
- Services receive repositories via constructor injection (never access `IMongoDatabase` directly).
- All business rules, validation, and orchestration logic lives here.

## Example

```csharp
using proto_back.Interfaces.IRepositories;
using proto_back.Interfaces.IServices;
using proto_back.Models.Entities;

namespace proto_back.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
}
```

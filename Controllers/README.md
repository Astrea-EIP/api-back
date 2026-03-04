# Controllers

This folder contains **API controller classes** that handle HTTP requests.

## Conventions

- One controller per resource (e.g., `UsersController.cs`, `ProductsController.cs`).
- Use plural nouns for controller names (maps to `/api/users`, `/api/products`).
- Controllers should be thin — delegate all business logic to services via interfaces.
- Inject `IService` interfaces, **never** repositories directly.
- Return appropriate HTTP status codes and use `ActionResult<T>` for typed responses.

## Example

```csharp
using Microsoft.AspNetCore.Mvc;
using proto_back.Interfaces.IServices;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;

namespace proto_back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(user);
    }
}
```

# DTOs / Requests

This folder contains **request body models** — the DTOs used to deserialize incoming HTTP request payloads.

## Conventions

- Name files with a `Request` suffix (e.g., `CreateUserRequest.cs`, `UpdateProductRequest.cs`).
- Use `[Required]`, `[StringLength]`, and other `DataAnnotations` for validation.
- These classes should **never** contain business logic.
- They are decoupled from the database entities to allow the API shape to evolve independently.

## Example

```csharp
using System.ComponentModel.DataAnnotations;

namespace proto_back.DTOs.Requests;

public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; set; } = null!;
}
```

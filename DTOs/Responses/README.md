# DTOs / Responses

This folder contains **response body models** — the DTOs returned by API endpoints.

## Conventions

- Name files with a `Response` suffix (e.g., `UserResponse.cs`, `ProductListResponse.cs`).
- Only include the fields the client needs — avoid leaking internal details (e.g., never expose raw MongoDB `ObjectId` internals).
- These classes are decoupled from database entities so the API surface can evolve independently.

## Example

```csharp
namespace proto_back.DTOs.Responses;

public class UserResponse
{
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
```

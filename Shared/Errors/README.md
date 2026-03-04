# Shared / Errors

This folder contains **shared error models** used across all controllers for consistent error responses.

## Conventions

- Use a standardized `ErrorResponse` class for all API error payloads.
- Models here are shared across the entire application — keep them generic and reusable.
- Follow the [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807) format when applicable.

## Example

```csharp
namespace proto_back.Shared.Errors;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = null!;
    public string? Detail { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

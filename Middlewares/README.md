# Middlewares

This folder contains **custom ASP.NET middleware** classes for the HTTP pipeline.

## Conventions

- One file per middleware (e.g., `AccessTokenMiddleware.cs`).
- Each middleware must have an `InvokeAsync` method.
- Register middleware in `Program.cs` using `app.UseMiddleware<T>()`.
- Keep middleware focused on a single cross-cutting concern.

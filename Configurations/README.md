# Configurations

This folder contains **strongly-typed configuration classes** bound to `appsettings.json` sections.

## Conventions

- Name files with a `Settings` suffix (e.g., `MongoDbSettings.cs`, `JwtSettings.cs`).
- Bind to `appsettings.json` sections using the Options pattern (`IOptions<T>`).
- No logic — these are pure POCO classes for configuration values.

## Example

```csharp
namespace proto_back.Configurations;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
}
```

Register in `Program.cs`:

```csharp
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));
```

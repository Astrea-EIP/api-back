# Models / Entities

This folder contains the **domain entity classes** that map directly to MongoDB documents.

## Conventions

- One file per entity, named after the entity (e.g., `User.cs`, `Product.cs`).
- Each class should use `[BsonId]` and `[BsonElement]` attributes from `MongoDB.Driver` for field mapping.
- Use `ObjectId` or `string` for the `Id` property depending on your preference.
- Keep entities free of business logic — they are pure data containers.

## Example

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace proto_back.Models.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

# OpenApi

This folder contains **Swashbuckle filters** used to shape the generated OpenAPI 3.1
document (`docs/api.json`, `/swagger/v1/swagger.json`) beyond what attributes and XML
doc comments cover on their own.

## Conventions

- One filter per concern (e.g., `EnumSchemaFilter.cs`, `FlagsEnumSchemaFilter.cs`).
- Schema filters implement `ISchemaFilter`; operation filters implement
  `IOperationFilter`, both from `Swashbuckle.AspNetCore.SwaggerGen`.
- Filters only shape the **document** — they must never change DTO runtime behavior
  (serialization, validation).
- Written against Swashbuckle.AspNetCore v10 / `Microsoft.OpenApi` 2.x: schema members
  are exposed through `IOpenApiSchema`, which is read-only. To mutate a schema, cast to
  the concrete `OpenApiSchema` first (`if (schema is not OpenApiSchema concrete) return;`).
- Register filters in `Program.cs`'s `AddSwaggerGen(...)` call.

## Example

```csharp
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace proto_back.Shared.OpenApi;

public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        concreteSchema.Description = "...";
    }
}
```

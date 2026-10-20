using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace proto_back.Shared.OpenApi;

/// <summary>
/// For plain (non-<see cref="FlagsAttribute"/>) integer-backed enums: keeps the
/// generated <c>type: integer</c> plus <c>enum</c> values, appends a value/name mapping
/// to the description, and adds <c>x-enum-varnames</c> so OpenAPI generators produce a
/// named enum instead of a bare integer.
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        var type = context.Type;
        if (!type.IsEnum || type.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            return;
        }

        var names = Enum.GetNames(type);
        var values = Enum.GetValues(type);

        var mapping = string.Join(", ", names.Select((name, i) => $"{Convert.ToInt64(values.GetValue(i))} = {name}"));
        concreteSchema.Description = string.IsNullOrEmpty(concreteSchema.Description)
            ? mapping
            : $"{concreteSchema.Description} ({mapping})";

        var varNames = new JsonArray(names.Select(name => (JsonNode)JsonValue.Create(name)).ToArray());
        concreteSchema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        concreteSchema.Extensions["x-enum-varnames"] = new JsonNodeExtension(varNames);
    }
}

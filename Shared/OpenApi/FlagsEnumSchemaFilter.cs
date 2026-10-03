using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace proto_back.Shared.OpenApi;

/// <summary>
/// For <see cref="FlagsAttribute"/> bitmask enums: removes <c>enum</c> (it would forbid
/// valid combinations like <c>24</c>), and documents the valid range and each bit in
/// <c>description</c> instead.
/// </summary>
public class FlagsEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        var type = context.Type;
        if (!type.IsEnum || !type.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            return;
        }

        var names = Enum.GetNames(type);
        var values = Enum.GetValues(type);

        long max = 0;
        var bits = new List<string>();
        for (var i = 0; i < names.Length; i++)
        {
            var value = Convert.ToInt64(values.GetValue(i));
            max |= value;
            if (value != 0)
            {
                bits.Add($"{names[i]}={value}");
            }
        }

        concreteSchema.Enum = null;
        concreteSchema.Type = JsonSchemaType.Integer;
        concreteSchema.Minimum = "0";
        concreteSchema.Maximum = max.ToString();

        var bitList = string.Join(", ", bits);
        var mapping = $"Bitmask: {bitList}. Combine with bitwise OR";

        if (bits.Count >= 2)
        {
            var exampleNames = names.Zip(values.Cast<object>(), (name, value) => (name, value: Convert.ToInt64(value)))
                .Where(nv => nv.value != 0)
                .TakeLast(2)
                .ToList();
            var exampleValue = exampleNames.Aggregate(0L, (acc, nv) => acc | nv.value);
            mapping += $", e.g. {string.Join(" | ", exampleNames.Select(nv => nv.name))} = {exampleValue}";
        }

        mapping += ".";

        concreteSchema.Description = string.IsNullOrEmpty(concreteSchema.Description)
            ? mapping
            : $"{concreteSchema.Description} {mapping}";
    }
}

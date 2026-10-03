using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace proto_back.Shared.OpenApi;

/// <summary>
/// Marks non-nullable value-type properties (e.g. <c>double Lat</c>) as <c>required</c>.
/// <c>SupportNonNullableReferenceTypes</c>/<c>NonNullableReferenceTypesAsRequired</c>
/// only look at nullable-reference-type annotations, so plain value types like
/// <c>PointResponse.Lat</c>/<c>Lng</c> would otherwise stay optional in the generated
/// schema even though they are always present. <see cref="Nullable{T}"/> properties
/// (e.g. <c>double?</c>) are left alone.
/// </summary>
public class RequiredValueTypeSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema || concreteSchema.Properties is null)
        {
            return;
        }

        var requiredValueTypeProperties = context.Type
            .GetProperties()
            .Where(p => p.PropertyType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) is null)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requiredValueTypeProperties.Count == 0)
        {
            return;
        }

        concreteSchema.Required ??= new HashSet<string>();
        foreach (var propertyName in concreteSchema.Properties.Keys)
        {
            if (requiredValueTypeProperties.Contains(propertyName))
            {
                concreteSchema.Required.Add(propertyName);
            }
        }
    }
}

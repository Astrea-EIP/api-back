using Microsoft.OpenApi;
using proto_back.Middlewares;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace proto_back.Shared.OpenApi;

/// <summary>
/// Attaches the <c>accessToken</c> security requirement, and the <c>401</c> response,
/// to every operation except the public routes <see cref="AccessTokenMiddleware"/>
/// itself exempts — reusing that list so the two never drift apart.
/// </summary>
public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public const string SchemeName = "accessToken";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = "/" + context.ApiDescription.RelativePath?.TrimStart('/');
        var isPublic = AccessTokenMiddleware.PublicPrefixes
            .Any(prefix => relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (isPublic)
        {
            return;
        }

        var schemeReference = new OpenApiSecuritySchemeReference(SchemeName, context.Document, null);
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [schemeReference] = new List<string>()
        });

        operation.Responses ??= new OpenApiResponses();
        if (!operation.Responses.ContainsKey("401"))
        {
            operation.Responses["401"] = new OpenApiResponse { Description = "The access-token header is missing or invalid." };
        }
    }
}

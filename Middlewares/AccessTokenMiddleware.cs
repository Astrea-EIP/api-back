using System.Text.Json;
using proto_back.Interfaces.IServices;
using proto_back.Shared.Errors;

namespace proto_back.Middlewares;

public class AccessTokenMiddleware
{
    private readonly RequestDelegate _next;
    private const string AccessTokenHeader = "access-token";

    // Routes that do not require authentication
    private static readonly string[] PublicPrefixes = { "/v0/auth/", "/swagger" };

    public AccessTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip authentication for public routes
        if (IsPublicRoute(path))
        {
            await _next(context);
            return;
        }

        // Check for access-token header
        if (!context.Request.Headers.TryGetValue(AccessTokenHeader, out var tokenValues)
            || string.IsNullOrWhiteSpace(tokenValues.FirstOrDefault()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var token = tokenValues.First()!;

        // Validate the token
        if (!authService.ValidateToken(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static bool IsPublicRoute(string path)
    {
        return PublicPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

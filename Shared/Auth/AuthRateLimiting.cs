using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using proto_back.Configurations;
using proto_back.Shared.Errors;

namespace proto_back.Shared.Auth;

/// <summary>
/// Rate limits of the public account routes: a fixed window per client IP address, one policy
/// for sign-in and one for sign-up. Above the limit the API answers 429 with a
/// <c>Retry-After</c> header and the usual <c>{ "error": "..." }</c> body.
/// </summary>
public static class AuthRateLimiting
{
    public const string LoginPolicy = "login";
    public const string RegisterPolicy = "register";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthRateLimitSettings>(configuration.GetSection("AuthRateLimit"));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(LoginPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<AuthRateLimitSettings>>().Value;
                return PerIpAddress(context, settings.LoginPermitLimit, settings.LoginWindowSeconds);
            });

            options.AddPolicy(RegisterPolicy, context =>
            {
                var settings = context.RequestServices.GetRequiredService<IOptions<AuthRateLimitSettings>>().Value;
                return PerIpAddress(context, settings.RegisterPermitLimit, settings.RegisterWindowSeconds);
            });

            options.OnRejected = async (rejected, cancellationToken) =>
            {
                var retryAfter = rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
                    ? value
                    : TimeSpan.FromSeconds(1);

                var response = rejected.HttpContext.Response;
                response.Headers.RetryAfter = TooManyRequestsException.RoundUpSeconds(retryAfter).ToString(CultureInfo.InvariantCulture);
                await response.WriteAsJsonAsync(
                    new ErrorResponse { Error = TooManyRequestsException.Message(retryAfter) },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Reads the client IP address from <c>X-Forwarded-For</c> when the request comes from a
    /// trusted reverse proxy (<c>AuthRateLimit:TrustedProxies</c>), then applies the limits.
    /// </summary>
    public static IApplicationBuilder UseAuthRateLimiting(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<IOptions<AuthRateLimitSettings>>().Value;
        if (settings.TrustedProxies.Length > 0)
        {
            var forwarded = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor };
            foreach (var proxy in settings.TrustedProxies)
            {
                forwarded.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            app.UseForwardedHeaders(forwarded);
        }

        return app.UseRateLimiter();
    }

    private static RateLimitPartition<string> PerIpAddress(HttpContext context, int permitLimit, int windowSeconds)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0
        });
    }
}

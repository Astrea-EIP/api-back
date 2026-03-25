using System.Text.Json;
using proto_back.Interfaces.IRepositories;
using proto_back.Models.Entities;
using proto_back.Shared.Errors;

namespace proto_back.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N");

            _logger.LogError(ex, "Unhandled exception caught. ErrorId: {ErrorId}", errorId);

            // Persist error log to MongoDB asynchronously (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();

                    var errorLog = new ErrorLog
                    {
                        ErrorId = errorId,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        RequestPath = context.Request.Path.Value ?? string.Empty,
                        HttpMethod = context.Request.Method,
                        OccurredAt = DateTime.UtcNow
                    };

                    await repo.CreateAsync(errorLog);
                }
                catch (Exception dbEx)
                {
                    // If MongoDB is down, log but don't lose the original error response
                    _logger.LogError(dbEx, "Failed to persist error log to MongoDB. ErrorId: {ErrorId}", errorId);
                }
            });

            // Return 500 with ServerErrorResponse
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ServerErrorResponse
            {
                Error = ex.Message,
                ErrorId = errorId
            };

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}

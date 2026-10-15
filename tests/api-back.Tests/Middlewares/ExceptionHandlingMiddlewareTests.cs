using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using proto_back.Interfaces.IRepositories;
using proto_back.Middlewares;
using proto_back.Tests.Support;

namespace proto_back.Tests.Middlewares;

[Trait("Category", "Unit")]
public class ExceptionHandlingMiddlewareTests
{
    private static (ExceptionHandlingMiddleware Middleware, InMemoryErrorLogRepository Repository) CreateMiddleware(
        RequestDelegate next, bool repositoryThrows = false)
    {
        var repository = new InMemoryErrorLogRepository { ThrowOnCreate = repositoryThrows };

        var services = new ServiceCollection();
        services.AddScoped<IErrorLogRepository>(_ => repository);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance, scopeFactory);
        return (middleware, repository);
    }

    private static DefaultHttpContext CreateContext(string path = "/v0/itinerary", string method = "POST")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonObject> ReadBodyAsJsonAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var text = await reader.ReadToEndAsync();
        return JsonNode.Parse(text)!.AsObject();
    }

    [Fact]
    public async Task InvokeAsync_NoException_LeavesResponseUntouchedAndNeverPersists()
    {
        var context = CreateContext();
        var (middleware, repository) = CreateMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Empty(repository.Logs);
    }

    [Fact]
    public async Task InvokeAsync_Exception_Returns500WithJsonContentType()
    {
        var context = CreateContext();
        var (middleware, _) = CreateMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_Exception_BodyHasExactlyErrorAndErrorIdKeys()
    {
        var context = CreateContext();
        var (middleware, _) = CreateMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsJsonAsync(context);

        Assert.Equal(new HashSet<string> { "error", "errorId" }, body.Select(kvp => kvp.Key).ToHashSet());
        Assert.Equal("boom", body["error"]!.GetValue<string>());
        Assert.Matches("^[0-9a-f]{32}$", body["errorId"]!.GetValue<string>());
    }

    [Fact]
    public async Task InvokeAsync_Exception_PersistsMatchingErrorLog()
    {
        var context = CreateContext(path: "/v0/itinerary", method: "POST");
        var (middleware, repository) = CreateMiddleware(_ => throw new InvalidOperationException("boom"));

        await middleware.InvokeAsync(context);
        var body = await ReadBodyAsJsonAsync(context);
        var errorId = body["errorId"]!.GetValue<string>();

        var persisted = await Poll.UntilAsync(() => repository.Logs.Any(l => l.ErrorId == errorId));
        Assert.True(persisted, "Expected the error log to be persisted within the timeout.");

        var log = repository.Logs.Single(l => l.ErrorId == errorId);
        Assert.Equal("boom", log.Message);
        Assert.NotNull(log.StackTrace);
        Assert.Equal("/v0/itinerary", log.RequestPath);
        Assert.Equal("POST", log.HttpMethod);
        Assert.True((DateTime.UtcNow - log.OccurredAt).Duration() < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task InvokeAsync_RepositoryThrows_StillReturns500AndDoesNotEscape()
    {
        var context = CreateContext();
        var (middleware, _) = CreateMiddleware(_ => throw new InvalidOperationException("boom"), repositoryThrows: true);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await ReadBodyAsJsonAsync(context);
        Assert.Equal("boom", body["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task InvokeAsync_TwoFailures_ProduceDifferentErrorIds()
    {
        var context1 = CreateContext();
        var (middleware1, _) = CreateMiddleware(_ => throw new InvalidOperationException("first"));
        await middleware1.InvokeAsync(context1);
        var errorId1 = (await ReadBodyAsJsonAsync(context1))["errorId"]!.GetValue<string>();

        var context2 = CreateContext();
        var (middleware2, _) = CreateMiddleware(_ => throw new InvalidOperationException("second"));
        await middleware2.InvokeAsync(context2);
        var errorId2 = (await ReadBodyAsJsonAsync(context2))["errorId"]!.GetValue<string>();

        Assert.NotEqual(errorId1, errorId2);
    }
}

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using proto_back.Interfaces.IRepositories;
using proto_back.Middlewares;
using proto_back.Shared.Errors;
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

    // Régression #86 : la sauvegarde se fait en arrière-plan, souvent après la fin de la requête.
    // On bloque la création du scope jusqu'à ce que le HttpContext soit libéré, comme en production.
    [Fact]
    public async Task InvokeAsync_Exception_PersistsErrorLogEvenAfterTheRequestHasCompleted()
    {
        var repository = new InMemoryErrorLogRepository();
        var services = new ServiceCollection();
        services.AddScoped<IErrorLogRepository>(_ => repository);
        var provider = services.BuildServiceProvider();
        using var gate = new ManualResetEventSlim(false);
        var scopeFactory = new GatedScopeFactory(provider.GetRequiredService<IServiceScopeFactory>(), gate);
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            scopeFactory);
        var context = CreateContext(path: "/v0/itinerary", method: "POST");

        await middleware.InvokeAsync(context);
        var errorId = (await ReadBodyAsJsonAsync(context))["errorId"]!.GetValue<string>();
        context.Uninitialize();
        gate.Set();

        var persisted = await Poll.UntilAsync(() => repository.Logs.Any(l => l.ErrorId == errorId));
        Assert.True(persisted, "Expected the error log to be persisted after the request completed.");

        var log = repository.Logs.Single(l => l.ErrorId == errorId);
        Assert.Equal("/v0/itinerary", log.RequestPath);
        Assert.Equal("POST", log.HttpMethod);
    }

    private sealed class GatedScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceScopeFactory _inner;
        private readonly ManualResetEventSlim _gate;

        public GatedScopeFactory(IServiceScopeFactory inner, ManualResetEventSlim gate)
        {
            _inner = inner;
            _gate = gate;
        }

        public IServiceScope CreateScope()
        {
            _gate.Wait(TimeSpan.FromSeconds(5));
            return _inner.CreateScope();
        }
    }

    [Fact]
    public async Task InvokeAsync_ApiException_ReturnsItsStatusAndReadableMessage()
    {
        var context = CreateContext();
        var (middleware, _) = CreateMiddleware(_ => throw new TestApiException(422, "Aucun trajet trouvé."));

        await middleware.InvokeAsync(context);
        var body = await ReadBodyAsJsonAsync(context);

        Assert.Equal(422, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal(new[] { "error" }, body.Select(kvp => kvp.Key));
        Assert.Equal("Aucun trajet trouvé.", body["error"]!.GetValue<string>());
    }

    [Fact]
    public async Task InvokeAsync_ApiException_IsNotStoredAsACrash()
    {
        var context = CreateContext();
        var (middleware, repository) = CreateMiddleware(_ => throw new TestApiException(400, "Adresse introuvable."));

        await middleware.InvokeAsync(context);
        await Task.Delay(200);

        Assert.Empty(repository.Logs);
    }

    private sealed class TestApiException : ApiException
    {
        public TestApiException(int statusCode, string message)
            : base(statusCode, message)
        {
        }
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

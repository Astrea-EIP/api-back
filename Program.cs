using MongoDB.Driver;
using proto_back.Configurations;
using proto_back.Interfaces.IRepositories;
using proto_back.Interfaces.IServices;
using proto_back.Middlewares;
using proto_back.Repositories;
using proto_back.Services;
using Microsoft.AspNetCore.Mvc;
using proto_back.Shared.Errors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = string.Join("; ", context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return new BadRequestObjectResult(new ErrorResponse { Error = errors });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MongoDB configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));

var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>()!;
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoSettings.DatabaseName));

// Register repositories
builder.Services.AddScoped<IErrorLogRepository, ErrorLogRepository>();

// Register application services
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["Nominatim:BaseUrl"] ?? "https://nominatim.openstreetmap.org";

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("User-Agent", "api-back/1.0 (contact: backend-team)");
});
builder.Services.AddScoped<IItineraryService, ItineraryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Global exception handling — MUST be first to catch all unhandled exceptions
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Custom middleware: validate access-token header on protected routes
app.UseMiddleware<AccessTokenMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

using System.Reflection;
using Microsoft.OpenApi;
using MongoDB.Driver;
using proto_back.Configurations;
using proto_back.Interfaces.IRepositories;
using proto_back.Interfaces.IServices;
using proto_back.Middlewares;
using proto_back.Repositories;
using proto_back.Services;
using proto_back.Shared.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Astrea API",
        Version = "v0",
        Description = "Computes accessibility-aware itineraries for ASTREA's Beta Test Plan. " +
                       "Call GET /v0/auth/anonymous to obtain a token, then send it as the " +
                       "access-token header on every other request."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile), includeControllerXmlComments: true);

    options.AddSecurityDefinition(SecurityRequirementsOperationFilter.SchemeName, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "access-token",
        Description = "Anonymous access token obtained from GET /v0/auth/anonymous. " +
                       "Send it as the access-token header on every other request."
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();

    options.CustomOperationIds(apiDescription =>
        (apiDescription.ActionDescriptor as ControllerActionDescriptor)?.AttributeRouteInfo?.Name);

    options.SupportNonNullableReferenceTypes();
    options.NonNullableReferenceTypesAsRequired();
    options.SchemaFilter<RequiredValueTypeSchemaFilter>();
    options.SchemaFilter<EnumSchemaFilter>();
    options.SchemaFilter<FlagsEnumSchemaFilter>();
});

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
    app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
    app.UseSwaggerUI(options => options.ConfigObject.PersistAuthorization = true);
}

app.UseHttpsRedirection();

// Custom middleware: validate access-token header on protected routes
app.UseMiddleware<AccessTokenMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

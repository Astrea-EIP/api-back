using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using proto_back.Interfaces.IRepositories;

namespace proto_back.Tests.Support;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> wired for end-to-end tests: points
/// GraphHopper/Nominatim at local WireMock servers (no real network call ever happens)
/// and replaces <see cref="IErrorLogRepository"/> with an in-memory fake so the
/// placeholder Mongo connection string in appsettings.json is never touched.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public string? GraphHopperBaseUrl { get; set; }
    public string? NominatimBaseUrl { get; set; }

    public InMemoryErrorLogRepository ErrorLogRepository { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>();

            if (GraphHopperBaseUrl is not null)
            {
                overrides["GraphHopper:BaseUrl"] = GraphHopperBaseUrl;
            }

            if (NominatimBaseUrl is not null)
            {
                overrides["Nominatim:BaseUrl"] = NominatimBaseUrl;
            }

            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IErrorLogRepository>();
            services.AddSingleton<IErrorLogRepository>(ErrorLogRepository);

            // Defensive: avoid a 307 if TestServer ever exposes an https address via
            // IServerAddressesFeature — there is no real certificate to redirect to.
            services.PostConfigure<HttpsRedirectionOptions>(options =>
            {
                options.HttpsPort = null;
            });
        });
    }
}

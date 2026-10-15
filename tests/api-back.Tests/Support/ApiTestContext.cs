namespace proto_back.Tests.Support;

/// <summary>
/// Bundles a fresh <see cref="ApiFactory"/> with its own GraphHopper/Nominatim WireMock
/// servers, so each end-to-end test gets an isolated app instance and can configure its
/// own stubs without interfering with other tests.
/// </summary>
public sealed class ApiTestContext : IDisposable
{
    public ApiFactory Factory { get; }
    public WireMockFixture GraphHopper { get; }
    public WireMockFixture Nominatim { get; }
    public HttpClient Client { get; }

    public static ApiTestContext Create(bool configureGraphHopper = true)
    {
        var graphHopper = new WireMockFixture();
        var nominatim = new WireMockFixture();
        var factory = new ApiFactory
        {
            GraphHopperBaseUrl = configureGraphHopper ? graphHopper.BaseUrl : null,
            NominatimBaseUrl = nominatim.BaseUrl
        };

        return new ApiTestContext(factory, graphHopper, nominatim);
    }

    private ApiTestContext(ApiFactory factory, WireMockFixture graphHopper, WireMockFixture nominatim)
    {
        Factory = factory;
        GraphHopper = graphHopper;
        Nominatim = nominatim;
        Client = factory.CreateClient();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GraphHopper.Dispose();
        Nominatim.Dispose();
    }
}

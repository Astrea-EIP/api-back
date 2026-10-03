using System.Net;
using proto_back.Services;
using proto_back.Tests.Support;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class NominatimGeocodingServiceTests
{
    private static (NominatimGeocodingService Service, StubHttpMessageHandler Handler) CreateService(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string json = "[]")
    {
        var handler = StubHttpMessageHandler.ReturningJson(statusCode, json);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://nominatim.test/") };
        return (new NominatimGeocodingService(client), handler);
    }

    [Fact]
    public async Task ResolvePointAsync_PlainCoordinates_ParsesWithoutHttpCall()
    {
        var (service, handler) = CreateService();

        var point = await service.ResolvePointAsync("48.8566,2.3522");

        Assert.Equal(48.8566, point.Lat);
        Assert.Equal(2.3522, point.Lng);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ResolvePointAsync_CoordinatesWithSurroundingWhitespace_ParsesWithoutHttpCall()
    {
        var (service, handler) = CreateService();

        var point = await service.ResolvePointAsync(" 48.8566 , 2.3522 ");

        Assert.Equal(48.8566, point.Lat);
        Assert.Equal(2.3522, point.Lng);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ResolvePointAsync_NegativeCoordinates_Parses()
    {
        var (service, handler) = CreateService();

        var point = await service.ResolvePointAsync("-33.8688,-151.2093");

        Assert.Equal(-33.8688, point.Lat);
        Assert.Equal(-151.2093, point.Lng);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ResolvePointAsync_CoordinatesUnderFrFrCulture_StillParsesWithInvariantCulture()
    {
        using var _ = new CultureScope("fr-FR");
        var (service, handler) = CreateService();

        var point = await service.ResolvePointAsync("48.8566,2.3522");

        Assert.Equal(48.8566, point.Lat);
        Assert.Equal(2.3522, point.Lng);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task ResolvePointAsync_Address_SendsExactEscapedQueryToNominatim()
    {
        var (service, handler) = CreateService(json: "[{\"lat\":\"48.8566\",\"lon\":\"2.3522\"}]");

        await service.ResolvePointAsync("Place de la Concorde, Paris");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(
            "/search?q=Place%20de%20la%20Concorde%2C%20Paris&format=json&limit=1",
            handler.LastRequest!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task ResolvePointAsync_Address_ParsesLatLonFromResult()
    {
        var (service, _) = CreateService(json: "[{\"lat\":\"48.8566\",\"lon\":\"2.3522\"}]");

        var point = await service.ResolvePointAsync("Place de la Concorde, Paris");

        Assert.Equal(48.8566, point.Lat);
        Assert.Equal(2.3522, point.Lng);
    }

    [Fact]
    public async Task ResolvePointAsync_EmptyResultArray_ThrowsArgumentExceptionContainingInput()
    {
        var (service, _) = CreateService(json: "[]");

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ResolvePointAsync("Nowhereland"));

        Assert.Contains("Nowhereland", ex.Message);
    }

    [Fact]
    public async Task ResolvePointAsync_UnparsableLatitude_ThrowsInvalidOperationException()
    {
        var (service, _) = CreateService(json: "[{\"lat\":\"not-a-number\",\"lon\":\"2.3522\"}]");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolvePointAsync("Somewhere"));
    }

    [Fact]
    public async Task ResolvePointAsync_HttpServerError_ThrowsHttpRequestException()
    {
        var (service, _) = CreateService(HttpStatusCode.InternalServerError, "[]");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.ResolvePointAsync("Somewhere"));
    }

    // Finding: French decimal commas are not recognized as coordinates (the split on
    // ',' sees 4 parts instead of 2, so TryParseCoordinate bails out) and the input is
    // sent to Nominatim as a plain address instead. Characterizes current behavior.
    [Fact]
    public async Task ResolvePointAsync_FrenchDecimalCommaCoordinates_AreTreatedAsAddressNotCoordinates()
    {
        var (service, handler) = CreateService(json: "[{\"lat\":\"48.8566\",\"lon\":\"2.3522\"}]");

        await service.ResolvePointAsync("48,8566,2,3522");

        Assert.Equal(1, handler.CallCount);
    }
}

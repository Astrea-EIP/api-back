using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Moq;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Services;
using proto_back.Tests.Support;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace proto_back.Tests.Integration;

/// <summary>
/// Exercises ItineraryService against the real AstreaEngine.dll, with GraphHopper
/// simulated by WireMock. No real network call is made. Only lat/lng swap, ordering
/// and error mapping are asserted here — engine internals (profile selection,
/// custom_model) are core-moteur's own test responsibility.
/// </summary>
[Trait("Category", "Integration")]
public class ItineraryEngineIntegrationTests : IDisposable
{
    private readonly WireMockFixture _graphHopper = new();

    private const string Start = "48.8566,2.3522";
    private const string End = "48.8606,2.3376";

    private static CreateItineraryRequest NewRequest() => new()
    {
        Start = Start,
        End = End,
        MobilityProfile = MobilityProfile.Pedestrian
    };

    private ItineraryService CreateService()
    {
        var geocoding = new Mock<IGeocodingService>();
        geocoding.Setup(g => g.ResolvePointAsync(Start))
            .ReturnsAsync(new PointResponse { Lat = 48.8566, Lng = 2.3522 });
        geocoding.Setup(g => g.ResolvePointAsync(End))
            .ReturnsAsync(new PointResponse { Lat = 48.8606, Lng = 2.3376 });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GraphHopper:BaseUrl"] = _graphHopper.BaseUrl
            })
            .Build();

        return new ItineraryService(config, geocoding.Object);
    }

    [Fact]
    public async Task ComputeItineraryAsync_HappyPath_ReturnsPointsInLatLngOrder()
    {
        _graphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"paths":[{"points":{"coordinates":[[2.3522,48.8566],[2.3376,48.8606]]}}]}"""));

        var result = await CreateService().ComputeItineraryAsync(NewRequest());

        Assert.Equal(2, result.Points.Count);
        Assert.Equal(48.8566, result.Points[0].Lat);
        Assert.Equal(2.3522, result.Points[0].Lng);
        Assert.Equal(48.8606, result.Points[1].Lat);
        Assert.Equal(2.3376, result.Points[1].Lng);

        var requests = _graphHopper.Server.LogEntries.ToList();
        Assert.Single(requests);

        var body = JsonNode.Parse(requests[0].RequestMessage.Body!)!.AsObject();
        var points = body["points"]!.AsArray();
        Assert.Equal(2.3522, points[0]![0]!.GetValue<double>());
        Assert.Equal(48.8566, points[0]![1]!.GetValue<double>());
        Assert.Equal(2.3376, points[1]![0]!.GetValue<double>());
        Assert.Equal(48.8606, points[1]![1]!.GetValue<double>());
    }

    [Fact]
    public async Task ComputeItineraryAsync_GraphHopperReturns500_ThrowsInvalidOperationException()
    {
        _graphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().ComputeItineraryAsync(NewRequest()));
    }

    [Fact]
    public async Task ComputeItineraryAsync_GraphHopperUnreachable_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        _graphHopper.Dispose(); // the configured base URL now refuses connections

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ComputeItineraryAsync(NewRequest()));
    }

    [Fact]
    public async Task ComputeItineraryAsync_GraphHopperReturnsNoPaths_ThrowsInvalidDataException()
    {
        _graphHopper.Server
            .Given(Request.Create().WithPath("/route").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"paths":[]}"""));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CreateService().ComputeItineraryAsync(NewRequest()));
    }

    public void Dispose()
    {
        _graphHopper.Dispose();
    }
}

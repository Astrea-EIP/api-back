using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Moq;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;
using proto_back.Services;
using proto_back.Tests.Support;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class ItineraryServiceTests
{
    private static IConfiguration ConfigWithGraphHopper(string? baseUrl = "http://graphhopper.test")
    {
        var values = new Dictionary<string, string?>();
        if (baseUrl is not null)
        {
            values["GraphHopper:BaseUrl"] = baseUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static CreateItineraryRequest NewRequest(string start = "48.8566,2.3522", string end = "48.8606,2.3376")
    {
        return new CreateItineraryRequest { Start = start, End = end, MobilityProfile = MobilityProfile.Pedestrian };
    }

    [Fact]
    public async Task ComputeItineraryAsync_MissingGraphHopperBaseUrl_ThrowsAndNeverCallsGeocoding()
    {
        var geocoding = new Mock<IGeocodingService>();
        var service = new ItineraryService(ConfigWithGraphHopper(null), geocoding.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ComputeItineraryAsync(NewRequest()));

        geocoding.Verify(g => g.ResolvePointAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ComputeItineraryAsync_StartResolutionThrows_PropagatesAndNeverResolvesEnd()
    {
        var request = NewRequest();
        var geocoding = new Mock<IGeocodingService>();
        geocoding.Setup(g => g.ResolvePointAsync(request.Start))
            .ThrowsAsync(new ArgumentException("cannot resolve start"));

        var service = new ItineraryService(ConfigWithGraphHopper(), geocoding.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ComputeItineraryAsync(request));

        geocoding.Verify(g => g.ResolvePointAsync(request.End), Times.Never);
    }

    [Theory]
    [InlineData(MobilityProfile.Pedestrian, "walking")]
    [InlineData(MobilityProfile.Wheelchair, "wheelchair")]
    [InlineData(MobilityProfile.Crutches, "crutches")]
    [InlineData(MobilityProfile.Blind, "blind")]
    public void BuildUserJson_OnlyMobilityProfileSet_SendsOnlyTheEnginePreset(MobilityProfile profile, string expectedPreset)
    {
        var request = new CreateItineraryRequest { Start = "a", End = "b", MobilityProfile = profile };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.Single(node);
        Assert.Equal(expectedPreset, node["profile"]!.GetValue<string>());
    }

    [Fact]
    public void BuildUserJson_WheelchairWidthSet_SendsItAsEngineWidth()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Wheelchair,
            WheelchairWidth = 0.8
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.Equal(0.8, node["width"]!.GetValue<double>());
    }

    [Fact]
    public void BuildUserJson_WheelchairWidthSet_UnderFrFrCulture_StillWritesInvariantDouble()
    {
        using var _ = new CultureScope("fr-FR");
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Wheelchair,
            WheelchairWidth = 0.8
        };

        var json = ItineraryService.BuildUserJson(request);

        Assert.Contains("\"width\":0.8", json);
    }

    [Theory]
    [InlineData(MobilityProfile.Pedestrian)]
    [InlineData(MobilityProfile.Crutches)]
    [InlineData(MobilityProfile.Blind)]
    public void BuildUserJson_WheelchairWidthOnOtherProfile_IsNotSent(MobilityProfile profile)
    {
        var request = new CreateItineraryRequest { Start = "a", End = "b", MobilityProfile = profile, WheelchairWidth = 0.8 };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.False(node.ContainsKey("width"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildUserJson_CrutchesWithCanClimbStairs_SendsCanTakeStairs(bool canClimbStairs)
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Crutches,
            CanClimbStairs = canClimbStairs
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.True(node.ContainsKey("can_take_stairs"));
        Assert.Equal(canClimbStairs, node["can_take_stairs"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData(MobilityProfile.Pedestrian)]
    [InlineData(MobilityProfile.Wheelchair)]
    [InlineData(MobilityProfile.Blind)]
    public void BuildUserJson_CanClimbStairsOnOtherProfile_IsNotSent(MobilityProfile profile)
    {
        var request = new CreateItineraryRequest { Start = "a", End = "b", MobilityProfile = profile, CanClimbStairs = true };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.False(node.ContainsKey("can_take_stairs"));
    }

    [Fact]
    public void BuildUserJson_PedestrianPrefersStairs_AsksTheEngineToAvoidSlopes()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Pedestrian,
            PathPreferences = PathPreference.Stairs
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.True(node["avoid_slopes"]!.GetValue<bool>());
        Assert.False(node.ContainsKey("avoid_stairs"));
    }

    [Fact]
    public void BuildUserJson_PedestrianPrefersSlopes_AsksTheEngineToAvoidStairs()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Pedestrian,
            PathPreferences = PathPreference.Slopes
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.True(node["avoid_stairs"]!.GetValue<bool>());
        Assert.False(node.ContainsKey("avoid_slopes"));
    }

    [Fact]
    public void BuildUserJson_PedestrianPrefersStairsAndSlopes_SendsNoPreference()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Pedestrian,
            PathPreferences = PathPreference.Stairs | PathPreference.Slopes
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.False(node.ContainsKey("avoid_stairs"));
        Assert.False(node.ContainsKey("avoid_slopes"));
    }

    [Theory]
    [InlineData(MobilityProfile.Wheelchair)]
    [InlineData(MobilityProfile.Crutches)]
    [InlineData(MobilityProfile.Blind)]
    public void BuildUserJson_PathPreferencesOnOtherProfile_AreNotSent(MobilityProfile profile)
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = profile,
            PathPreferences = PathPreference.Stairs
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.False(node.ContainsKey("avoid_stairs"));
        Assert.False(node.ContainsKey("avoid_slopes"));
    }

    [Fact]
    public void ToEnginePreset_UnknownProfile_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItineraryService.ToEnginePreset((MobilityProfile)42));
    }
}

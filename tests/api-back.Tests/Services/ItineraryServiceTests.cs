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
    [InlineData(MobilityProfile.Pedestrian)]
    [InlineData(MobilityProfile.Wheelchair)]
    [InlineData(MobilityProfile.Crutches)]
    [InlineData(MobilityProfile.Blind)]
    public void BuildUserJson_OnlyMobilityProfileSet_ProducesExactlyOneKey(MobilityProfile profile)
    {
        var request = new CreateItineraryRequest { Start = "a", End = "b", MobilityProfile = profile };

        var json = ItineraryService.BuildUserJson(request);
        var node = JsonNode.Parse(json)!.AsObject();

        Assert.Single(node);
        Assert.Equal((int)profile, node["mobility_profile"]!.GetValue<int>());
    }

    [Fact]
    public void BuildUserJson_PathPreferencesSet_WritesCombinedFlagsAsInteger()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Pedestrian,
            PathPreferences = PathPreference.LitStreets | PathPreference.Benches
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.Equal(24, node["path_preferences"]!.GetValue<int>());
    }

    [Fact]
    public void BuildUserJson_WheelchairWidthSet_WritesDoubleValue()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Wheelchair,
            WheelchairWidth = 0.8
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.Equal(0.8, node["wheelchair_width"]!.GetValue<double>());
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

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.Equal(0.8, node["wheelchair_width"]!.GetValue<double>());
    }

    [Fact]
    public void BuildUserJson_PhysicalStrengthSet_WritesIntegerEnumValue()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Wheelchair,
            PhysicalStrength = PhysicalStrength.High
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.Equal(2, node["physical_strength"]!.GetValue<int>());
    }

    [Fact]
    public void BuildUserJson_IsAccompaniedTrue_WritesTrue()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Blind,
            IsAccompanied = true
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.True(node["is_accompanied"]!.GetValue<bool>());
    }

    [Fact]
    public void BuildUserJson_CanClimbStairsFalse_IsPresentAsFalseNotOmitted()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Crutches,
            CanClimbStairs = false
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.True(node.ContainsKey("can_climb_stairs"));
        Assert.False(node["can_climb_stairs"]!.GetValue<bool>());
    }

    [Fact]
    public void BuildUserJson_OptionalFieldsNull_AreOmitted()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Pedestrian
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        Assert.False(node.ContainsKey("path_preferences"));
        Assert.False(node.ContainsKey("wheelchair_width"));
        Assert.False(node.ContainsKey("physical_strength"));
        Assert.False(node.ContainsKey("is_accompanied"));
        Assert.False(node.ContainsKey("can_climb_stairs"));
    }

    [Fact]
    public void BuildUserJson_AllFieldsSet_KeysAreExactlyTheSixDocumentedOnes()
    {
        var request = new CreateItineraryRequest
        {
            Start = "a",
            End = "b",
            MobilityProfile = MobilityProfile.Wheelchair,
            PathPreferences = PathPreference.Stairs,
            WheelchairWidth = 0.7,
            PhysicalStrength = PhysicalStrength.Medium,
            IsAccompanied = false,
            CanClimbStairs = true
        };

        var node = JsonNode.Parse(ItineraryService.BuildUserJson(request))!.AsObject();

        var expectedKeys = new HashSet<string>
        {
            "mobility_profile", "path_preferences", "wheelchair_width",
            "physical_strength", "is_accompanied", "can_climb_stairs"
        };

        Assert.Equal(expectedKeys, node.Select(kvp => kvp.Key).ToHashSet());
    }
}

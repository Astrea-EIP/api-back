using proto_back.DTOs.Responses;
using proto_back.Services;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class WheelchairServiceTests
{
    private static readonly WheelchairCatalogResponse Catalog = new WheelchairService().GetCatalog();

    private static IEnumerable<WheelchairModelResponse> AllModels =>
        Catalog.Categories.SelectMany(c => c.Models);

    [Fact]
    public void GetCatalog_Always_ReturnsManualThenElectricCategories()
    {
        Assert.Equal(new[] { "manual", "electric" }, Catalog.Categories.Select(c => c.Id));
        Assert.Equal(4, Catalog.Categories[0].Models.Count);
        Assert.Equal(5, Catalog.Categories[1].Models.Count);
    }

    [Fact]
    public void GetCatalog_Always_ReturnsUniqueModelIds()
    {
        var ids = AllModels.Select(m => m.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void GetCatalog_Always_ReturnsLowercaseHyphenatedIds()
    {
        var ids = Catalog.Categories.Select(c => c.Id).Concat(AllModels.Select(m => m.Id));

        Assert.All(ids, id => Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", id));
    }

    [Fact]
    public void GetCatalog_KnownModel_ReturnsDimensionsInMeters()
    {
        var model = AllModels.Single(m => m.Id == "invacare-action-kuschall");

        Assert.Equal("Invacare", model.Brand);
        Assert.Equal("Action, Küschall", model.Range);
        Assert.Equal(0.55, model.Width.Min);
        Assert.Equal(0.7, model.Width.Max);
        Assert.Equal(0.85, model.Length.Min);
        Assert.Equal(1.05, model.Length.Max);
    }

    [Fact]
    public void GetCatalog_Always_ReturnsConsistentRanges()
    {
        Assert.All(AllModels, m =>
        {
            Assert.True(m.Width.Min > 0 && m.Width.Min <= m.Width.Max, $"{m.Id} width");
            Assert.True(m.Length.Min > 0 && m.Length.Min <= m.Length.Max, $"{m.Id} length");
        });
    }

    [Fact]
    public void GetCatalog_Always_RecommendsTheMaximumWidth()
    {
        Assert.All(AllModels, m => Assert.Equal(m.Width.Max, m.RecommendedWheelchairWidth));
    }

    [Fact]
    public void GetCatalog_Always_RecommendsAWidthAcceptedByTheItineraryRequest()
    {
        // CreateItineraryRequest.WheelchairWidth is validated as > 0 and <= 2.
        Assert.All(AllModels, m => Assert.InRange(m.RecommendedWheelchairWidth, 0.01, 2.0));
    }

    [Fact]
    public void GetCatalog_Always_ReturnsAccessibilityStandardsInMeters()
    {
        Assert.Equal(0.8, Catalog.AccessibilityStandards.MinPassageWidth);
        Assert.Equal(0.9, Catalog.AccessibilityStandards.RecommendedCorridorWidth);
        Assert.Equal(1.5, Catalog.AccessibilityStandards.TurningCircleDiameter);
    }
}

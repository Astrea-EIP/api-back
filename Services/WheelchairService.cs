using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;

namespace proto_back.Services;

/// <summary>
/// Catalogue of standard wheelchairs and spatial accessibility reference values.
/// The apps build their wheelchair selector from it instead of hardcoding the list:
/// a model is added or corrected here, without a new mobile/web release.
/// </summary>
public class WheelchairService : IWheelchairService
{
    // Dimensions in centimeters, as published by the manufacturers. Converted to meters,
    // the unit of wheelchair_width on POST /v0/itinerary, when building the response.
    private static readonly IReadOnlyList<CategoryData> Categories = new[]
    {
        new CategoryData("manual", "Manual wheelchairs", new[]
        {
            new ModelData("invacare-action-kuschall", "Invacare", "Action, Küschall", 55, 70, 85, 105),
            new ModelData("sunrise-quickie-breezy", "Sunrise Medical", "Quickie, Breezy", 52, 68, 80, 100),
            new ModelData("ottobock-motus-avantgarde", "Ottobock", "Motus, Avantgarde", 58, 65, 85, 105),
            new ModelData("tilite-active", "TiLite", "Active titanium/aluminium models", 50, 65, 75, 95),
        }),
        new CategoryData("electric", "Electric wheelchairs", new[]
        {
            new ModelData("permobil-f-m", "Permobil", "F and M series", 61, 66, 95, 120),
            // 58–62 cm for indoor models, up to 70 cm for outdoor models.
            new ModelData("pride-quantum", "Pride Mobility", "Quantum", 58, 70, 90, 110),
            new ModelData("invacare-storm-tdx", "Invacare", "Storm, TDX", 61, 65, 98, 116),
            new ModelData("sunrise-q-series", "Sunrise Medical", "Q-Series", 60, 66, 100, 115),
            new ModelData("meyra-ichair", "Meyra", "iChair", 58, 65, 95, 113),
        }),
    };

    private const int MinPassageWidthCm = 80;
    private const int RecommendedCorridorWidthCm = 90;
    private const int TurningCircleDiameterCm = 150;

    public WheelchairCatalogResponse GetCatalog()
    {
        return new WheelchairCatalogResponse
        {
            Categories = Categories.Select(category => new WheelchairCategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Models = category.Models.Select(ToResponse).ToList()
            }).ToList(),
            AccessibilityStandards = new AccessibilityStandardsResponse
            {
                MinPassageWidth = ToMeters(MinPassageWidthCm),
                RecommendedCorridorWidth = ToMeters(RecommendedCorridorWidthCm),
                TurningCircleDiameter = ToMeters(TurningCircleDiameterCm)
            }
        };
    }

    private static WheelchairModelResponse ToResponse(ModelData model) => new()
    {
        Id = model.Id,
        Brand = model.Brand,
        Range = model.Range,
        Width = new DimensionRangeResponse { Min = ToMeters(model.MinWidthCm), Max = ToMeters(model.MaxWidthCm) },
        Length = new DimensionRangeResponse { Min = ToMeters(model.MinLengthCm), Max = ToMeters(model.MaxLengthCm) },
        // The widest chair of the range: a path wide enough for it fits every chair of the range.
        RecommendedWheelchairWidth = ToMeters(model.MaxWidthCm)
    };

    // Integer centimeters divided by 100 give exact decimal values in JSON (0.7, not 0.7000000000000001).
    private static double ToMeters(int centimeters) => centimeters / 100.0;

    private sealed record CategoryData(string Id, string Name, IReadOnlyList<ModelData> Models);

    private sealed record ModelData(
        string Id,
        string Brand,
        string Range,
        int MinWidthCm,
        int MaxWidthCm,
        int MinLengthCm,
        int MaxLengthCm);
}

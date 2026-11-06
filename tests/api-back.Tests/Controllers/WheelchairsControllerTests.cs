using Microsoft.AspNetCore.Mvc;
using Moq;
using proto_back.Controllers;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;

namespace proto_back.Tests.Controllers;

[Trait("Category", "Unit")]
public class WheelchairsControllerTests
{
    [Fact]
    public void GetWheelchairs_Always_ReturnsOkWithServiceCatalog()
    {
        var catalog = new WheelchairCatalogResponse
        {
            AccessibilityStandards = new AccessibilityStandardsResponse { MinPassageWidth = 0.8 }
        };
        var wheelchairService = new Mock<IWheelchairService>();
        wheelchairService.Setup(s => s.GetCatalog()).Returns(catalog);
        var controller = new WheelchairsController(wheelchairService.Object);

        var result = controller.GetWheelchairs();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(catalog, ok.Value);
    }
}

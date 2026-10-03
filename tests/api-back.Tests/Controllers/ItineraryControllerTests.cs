using Microsoft.AspNetCore.Mvc;
using Moq;
using proto_back.Controllers;
using proto_back.DTOs.Requests;
using proto_back.DTOs.Responses;
using proto_back.Interfaces.IServices;

namespace proto_back.Tests.Controllers;

[Trait("Category", "Unit")]
public class ItineraryControllerTests
{
    [Fact]
    public async Task PostItinerary_Always_Returns201WithServiceResultAndCallsServiceOnce()
    {
        var request = new CreateItineraryRequest { Start = "a", End = "b", MobilityProfile = MobilityProfile.Pedestrian };
        var expected = new ItineraryResponse
        {
            Points = new List<PointResponse> { new() { Lat = 1, Lng = 2 } }
        };

        var service = new Mock<IItineraryService>();
        service.Setup(s => s.ComputeItineraryAsync(request)).ReturnsAsync(expected);
        var controller = new ItineraryController(service.Object);

        var result = await controller.PostItinerary("token", request);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.Same(expected, objectResult.Value);
        service.Verify(s => s.ComputeItineraryAsync(request), Times.Once);
    }
}

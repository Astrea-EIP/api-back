using proto_back.Services;
using proto_back.Shared.Errors;
using proto_back.Tests.Support;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class GraphHopperErrorTranslatorTests
{
    // The engine relays GraphHopper errors as "API error: HTTP 400: <body>".
    private static InvalidOperationException EngineError(string body) => new($"API error: HTTP 400: {body}");

    [Theory]
    [InlineData(0, "Le point de départ")]
    [InlineData(1, "Le point d'arrivée")]
    public void TryTranslate_PointOutOfBounds_ReturnsPointOutOfMapNamingThePoint(int pointIndex, string expectedStart)
    {
        var translated = GraphHopperErrorTranslator.TryTranslate(
            EngineError(GraphHopperErrorBodies.PointOutOfBounds(pointIndex)), out var apiException);

        Assert.True(translated);
        var error = Assert.IsType<PointOutOfMapException>(apiException);
        Assert.Equal(422, error.StatusCode);
        Assert.StartsWith(expectedStart, error.Message);
    }

    [Fact]
    public void TryTranslate_PointNotFound_ReturnsPointOutOfMap()
    {
        var translated = GraphHopperErrorTranslator.TryTranslate(
            EngineError(GraphHopperErrorBodies.PointNotFound(1)), out var apiException);

        Assert.True(translated);
        Assert.IsType<PointOutOfMapException>(apiException);
        Assert.StartsWith("Le point d'arrivée", apiException!.Message);
    }

    [Fact]
    public void TryTranslate_PointError_DoesNotLeakTheRawGraphHopperBody()
    {
        GraphHopperErrorTranslator.TryTranslate(
            EngineError(GraphHopperErrorBodies.PointOutOfBounds(0)), out var apiException);

        Assert.DoesNotContain("graphhopper", apiException!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bounds", apiException.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryTranslate_ConnectionNotFound_ReturnsRouteNotFound()
    {
        var translated = GraphHopperErrorTranslator.TryTranslate(
            EngineError(GraphHopperErrorBodies.ConnectionNotFound), out var apiException);

        Assert.True(translated);
        var error = Assert.IsType<RouteNotFoundException>(apiException);
        Assert.Equal(422, error.StatusCode);
        Assert.DoesNotContain("Connection between locations", error.Message);
    }

    [Fact]
    public void TryTranslate_MaximumNodesExceeded_ReturnsRouteNotFound()
    {
        var translated = GraphHopperErrorTranslator.TryTranslate(
            EngineError(GraphHopperErrorBodies.MaximumNodesExceeded), out var apiException);

        Assert.True(translated);
        Assert.IsType<RouteNotFoundException>(apiException);
    }

    [Theory]
    [InlineData("API error: HTTP 500: boom")]
    [InlineData("API error: HTTP error: Connection refused")]
    [InlineData("API error: Request timeout")]
    public void TryTranslate_OtherErrors_AreNotTranslated(string message)
    {
        var translated = GraphHopperErrorTranslator.TryTranslate(new InvalidOperationException(message), out var apiException);

        Assert.False(translated);
        Assert.Null(apiException);
    }
}

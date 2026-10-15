namespace proto_back.Tests.Support;

/// <summary>
/// Error bodies returned by GraphHopper 10.2 (HTTP 400), in the format produced by its
/// MultiExceptionSerializer: a top-level message and one hint per error with the Java class name.
/// </summary>
public static class GraphHopperErrorBodies
{
    public static string PointOutOfBounds(int pointIndex) =>
        $$"""{"message":"Point {{pointIndex}} is out of bounds: 50.4501,30.5234, the bounds are: -1.9,4.6,46.2,48.7,0.0,0.0","hints":[{"message":"Point {{pointIndex}} is out of bounds: 50.4501,30.5234, the bounds are: -1.9,4.6,46.2,48.7,0.0,0.0","details":"com.graphhopper.util.exceptions.PointOutOfBoundsException","point_index":{{pointIndex}}}]}""";

    public static string PointNotFound(int pointIndex) =>
        $$"""{"message":"Cannot find point {{pointIndex}}: 47.2184,-1.5536","hints":[{"message":"Cannot find point {{pointIndex}}: 47.2184,-1.5536","details":"com.graphhopper.util.exceptions.PointNotFoundException","point_index":{{pointIndex}}}]}""";

    public const string ConnectionNotFound =
        """{"message":"Connection between locations not found","hints":[{"message":"Connection between locations not found","details":"com.graphhopper.util.exceptions.ConnectionNotFoundException"}]}""";

    public const string MaximumNodesExceeded =
        """{"message":"No path found due to maximum nodes exceeded 1000000","hints":[{"message":"No path found due to maximum nodes exceeded 1000000","details":"com.graphhopper.util.exceptions.MaximumNodesExceededException","max_visited_nodes":1000000}]}""";
}

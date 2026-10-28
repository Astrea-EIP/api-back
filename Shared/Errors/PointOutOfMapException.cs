namespace proto_back.Shared.Errors;

/// <summary>
/// The start or end point is outside the area covered by the routing map, or too far from any
/// path. Returned as a 422.
/// </summary>
public sealed class PointOutOfMapException : ApiException
{
    public PointOutOfMapException(string message, Exception? innerException = null)
        : base(StatusCodes.Status422UnprocessableEntity, message, innerException)
    {
    }
}

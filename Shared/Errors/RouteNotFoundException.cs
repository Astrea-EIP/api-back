namespace proto_back.Shared.Errors;

/// <summary>
/// No route matching the mobility profile was found between the two points. Returned as a 422.
/// </summary>
public sealed class RouteNotFoundException : ApiException
{
    public RouteNotFoundException(string message, Exception? innerException = null)
        : base(StatusCodes.Status422UnprocessableEntity, message, innerException)
    {
    }
}

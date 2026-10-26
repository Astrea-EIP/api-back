namespace proto_back.Shared.Errors;

/// <summary>
/// The start or end address could not be turned into coordinates. Returned as a 400.
/// </summary>
public sealed class AddressNotFoundException : ApiException
{
    public AddressNotFoundException(string message)
        : base(StatusCodes.Status400BadRequest, message)
    {
    }
}

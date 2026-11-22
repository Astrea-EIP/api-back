namespace proto_back.Shared.Errors;

/// <summary>
/// An account already exists with this email address. Returned as a 409.
/// </summary>
public sealed class EmailAlreadyUsedException : ApiException
{
    public EmailAlreadyUsedException()
        : base(StatusCodes.Status409Conflict, "An account already exists with this email address.")
    {
    }
}

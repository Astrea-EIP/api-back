namespace proto_back.Shared.Errors;

/// <summary>
/// The email address or the password is wrong. Returned as a 401. The message is the same in
/// both cases, so that it cannot be used to find out whether an email address has an account.
/// </summary>
public sealed class InvalidCredentialsException : ApiException
{
    public InvalidCredentialsException()
        : base(StatusCodes.Status401Unauthorized, "The email address or the password is wrong.")
    {
    }
}

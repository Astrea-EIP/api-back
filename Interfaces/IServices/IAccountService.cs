using proto_back.DTOs.Requests;

namespace proto_back.Interfaces.IServices;

public interface IAccountService
{
    /// <summary>
    /// Creates the account and returns a user token for it.
    /// Throws <see cref="Shared.Errors.EmailAlreadyUsedException"/> when the email address is taken.
    /// </summary>
    Task<string> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Checks the email address and the password and returns a user token.
    /// Throws <see cref="Shared.Errors.InvalidCredentialsException"/> when either is wrong.
    /// </summary>
    Task<string> LoginAsync(LoginRequest request);
}

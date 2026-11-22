using Microsoft.AspNetCore.Identity;
using proto_back.DTOs.Requests;
using proto_back.Interfaces.IRepositories;
using proto_back.Interfaces.IServices;
using proto_back.Models.Entities;
using proto_back.Shared.Errors;

namespace proto_back.Services;

/// <summary>
/// Creates accounts and signs users in. Passwords are hashed with the ASP.NET Core Identity
/// <see cref="IPasswordHasher{TUser}"/> (PBKDF2 with a random salt) and never stored in clear.
/// </summary>
public class AccountService : IAccountService
{
    private readonly IUserRepository _users;
    private readonly IAuthService _authService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TimeProvider _timeProvider;

    // Hash checked when the email address has no account, so that a sign-in with an unknown
    // address takes as long as one with a wrong password.
    private readonly Lazy<string> _unknownUserHash;

    public AccountService(
        IUserRepository users,
        IAuthService authService,
        IPasswordHasher<User> passwordHasher,
        TimeProvider? timeProvider = null)
    {
        _users = users;
        _authService = authService;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _unknownUserHash = new Lazy<string>(() => _passwordHasher.HashPassword(new User(), Guid.NewGuid().ToString("N")));
    }

    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            Name = request.Name.Trim(),
            Email = NormalizeEmail(request.Email),
            BirthDate = request.BirthDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        if (!await _users.TryCreateAsync(user))
        {
            throw new EmailAlreadyUsedException();
        }

        return _authService.GenerateUserToken(user.Id);
    }

    public async Task<string> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email));

        if (user is null)
        {
            _passwordHasher.VerifyHashedPassword(new User(), _unknownUserHash.Value, request.Password);
            throw new InvalidCredentialsException();
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        return _authService.GenerateUserToken(user.Id);
    }

    /// <summary>Email addresses are compared without case and without surrounding spaces.</summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

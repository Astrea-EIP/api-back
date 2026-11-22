namespace proto_back.Configurations;

/// <summary>
/// Limits on the public account routes (section <c>AuthRateLimit</c>), against password guessing
/// and mass account creation. The counters are kept in memory, per API instance.
/// </summary>
public class AuthRateLimitSettings
{
    /// <summary>Sign-in requests allowed per client IP address in each <see cref="LoginWindowSeconds"/> window.</summary>
    public int LoginPermitLimit { get; set; } = 5;

    public int LoginWindowSeconds { get; set; } = 60;

    /// <summary>Sign-up requests allowed per client IP address in each <see cref="RegisterWindowSeconds"/> window.</summary>
    public int RegisterPermitLimit { get; set; } = 10;

    public int RegisterWindowSeconds { get; set; } = 3600;

    /// <summary>
    /// Failed sign-ins allowed for one email address, from any IP address, in each
    /// <see cref="FailedLoginWindowSeconds"/> window. Above it, the address is blocked until the
    /// window ends, even with the right password.
    /// </summary>
    public int FailedLoginsPerEmail { get; set; } = 10;

    public int FailedLoginWindowSeconds { get; set; } = 900;

    /// <summary>
    /// IP addresses of the reverse proxies allowed to set <c>X-Forwarded-For</c>. Empty: the
    /// header is ignored and the address of the TCP connection is used.
    /// </summary>
    public string[] TrustedProxies { get; set; } = [];
}

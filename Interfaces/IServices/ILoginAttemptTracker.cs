namespace proto_back.Interfaces.IServices;

/// <summary>
/// Counts failed sign-ins per email address, so that an address attacked from many IP
/// addresses gets blocked for a while.
/// </summary>
public interface ILoginAttemptTracker
{
    /// <summary>Whether the address is blocked, and for how long.</summary>
    bool IsBlocked(string email, out TimeSpan retryAfter);

    void RecordFailure(string email);

    /// <summary>Forgets the failures of the address, after a successful sign-in.</summary>
    void Reset(string email);
}

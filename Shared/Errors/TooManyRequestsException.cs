namespace proto_back.Shared.Errors;

/// <summary>
/// Too many attempts in a short time. Returned as a 429 with a <c>Retry-After</c> header.
/// </summary>
public sealed class TooManyRequestsException : ApiException
{
    public TooManyRequestsException(TimeSpan retryAfter)
        : base(StatusCodes.Status429TooManyRequests, Message(retryAfter))
    {
        RetryAfterSeconds = RoundUpSeconds(retryAfter);
    }

    /// <summary>Seconds to wait before trying again, at least 1.</summary>
    public int RetryAfterSeconds { get; }

    public static int RoundUpSeconds(TimeSpan retryAfter) => Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

    public static string Message(TimeSpan retryAfter) =>
        $"Too many attempts. Try again in {RoundUpSeconds(retryAfter)} seconds.";
}

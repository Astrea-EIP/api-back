namespace proto_back.Tests.Support;

/// <summary>
/// Polls a condition with a short timeout, for asserting on fire-and-forget work
/// (e.g. <c>ExceptionHandlingMiddleware</c>'s background error-log write) without
/// using <c>Thread.Sleep</c>.
/// </summary>
public static class Poll
{
    public static async Task<bool> UntilAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? interval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        var delay = interval ?? TimeSpan.FromMilliseconds(25);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(delay);
        }

        return condition();
    }
}

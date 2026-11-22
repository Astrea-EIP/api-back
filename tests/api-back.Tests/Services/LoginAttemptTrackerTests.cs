using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using proto_back.Configurations;
using proto_back.Services;

namespace proto_back.Tests.Services;

[Trait("Category", "Unit")]
public class LoginAttemptTrackerTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 22, 14, 0, 0, TimeSpan.Zero));
    private readonly LoginAttemptTracker _tracker;

    public LoginAttemptTrackerTests()
    {
        var settings = Options.Create(new AuthRateLimitSettings { FailedLoginsPerEmail = 3, FailedLoginWindowSeconds = 600 });
        _tracker = new LoginAttemptTracker(settings, _time);
    }

    private void Fail(string email, int times)
    {
        for (var i = 0; i < times; i++)
        {
            _tracker.RecordFailure(email);
        }
    }

    [Fact]
    public void IsBlocked_BelowTheLimit_IsFalse()
    {
        Fail("a@example.com", 2);

        Assert.False(_tracker.IsBlocked("a@example.com", out _));
    }

    [Fact]
    public void IsBlocked_AtTheLimit_IsTrueUntilTheEndOfTheWindow()
    {
        Fail("a@example.com", 3);
        _time.Advance(TimeSpan.FromSeconds(100));

        Assert.True(_tracker.IsBlocked("a@example.com", out var retryAfter));
        Assert.Equal(TimeSpan.FromSeconds(500), retryAfter);
    }

    [Fact]
    public void IsBlocked_AfterTheWindow_IsFalseAndCountsAgainFromZero()
    {
        Fail("a@example.com", 3);
        _time.Advance(TimeSpan.FromSeconds(600));

        Assert.False(_tracker.IsBlocked("a@example.com", out _));
        Fail("a@example.com", 2);
        Assert.False(_tracker.IsBlocked("a@example.com", out _));
    }

    [Fact]
    public void Reset_ForgetsTheFailures()
    {
        Fail("a@example.com", 3);

        _tracker.Reset("a@example.com");

        Assert.False(_tracker.IsBlocked("a@example.com", out _));
    }

    [Fact]
    public void RecordFailure_OnlyCountsForThatEmail()
    {
        Fail("a@example.com", 3);

        Assert.False(_tracker.IsBlocked("b@example.com", out _));
    }
}

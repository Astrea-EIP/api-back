using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using proto_back.Configurations;
using proto_back.Interfaces.IServices;

namespace proto_back.Services;

/// <summary>
/// In-memory <see cref="ILoginAttemptTracker"/>: a fixed window per email address, started by
/// its first failure. Entries expire with their window, so the memory used stays bounded by
/// the addresses tried recently. Counters are per API instance and reset when it restarts.
/// </summary>
public sealed class LoginAttemptTracker : ILoginAttemptTracker, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly object _lock = new();
    private readonly AuthRateLimitSettings _settings;
    private readonly TimeProvider _timeProvider;

    public LoginAttemptTracker(IOptions<AuthRateLimitSettings> settings, TimeProvider? timeProvider = null)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private TimeSpan Window => TimeSpan.FromSeconds(_settings.FailedLoginWindowSeconds);

    public bool IsBlocked(string email, out TimeSpan retryAfter)
    {
        lock (_lock)
        {
            retryAfter = TimeSpan.Zero;
            var entry = CurrentEntry(email);
            if (entry is null || entry.Failures < _settings.FailedLoginsPerEmail)
            {
                return false;
            }

            retryAfter = entry.WindowEnd - _timeProvider.GetUtcNow();
            return true;
        }
    }

    public void RecordFailure(string email)
    {
        lock (_lock)
        {
            var entry = CurrentEntry(email);
            if (entry is null)
            {
                entry = new Entry { WindowEnd = _timeProvider.GetUtcNow() + Window };
                // The entry is only removed from memory a little after its window, which is
                // checked against the TimeProvider anyway.
                _cache.Set(email, entry, Window + TimeSpan.FromMinutes(1));
            }

            entry.Failures++;
        }
    }

    public void Reset(string email)
    {
        lock (_lock)
        {
            _cache.Remove(email);
        }
    }

    public void Dispose() => _cache.Dispose();

    private Entry? CurrentEntry(string email)
    {
        if (!_cache.TryGetValue(email, out Entry? entry) || entry is null)
        {
            return null;
        }

        if (_timeProvider.GetUtcNow() >= entry.WindowEnd)
        {
            _cache.Remove(email);
            return null;
        }

        return entry;
    }

    private sealed class Entry
    {
        public int Failures { get; set; }

        public DateTimeOffset WindowEnd { get; init; }
    }
}

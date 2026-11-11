using System.Security.Cryptography;
using System.Text;
using Ical.Net;

namespace Hive.Infrastructure.Services;

/// <summary>
/// Holds the last Google Calendar feed fetched, parsed, for <see cref="Lifetime"/>, so a sync
/// or a change of month does not ask Google again. Registered as a singleton; lost on restart.
/// </summary>
/// <remarks>
/// Only one calendar is ever configured, so only one feed is held. It is keyed by a hash of
/// its secret address: saving a new address misses the cache instead of showing the old calendar.
/// </remarks>
public sealed class GoogleCalendarFeedCache
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private CachedGoogleCalendarFeed? _feed;

    public GoogleCalendarFeedCache(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// The feed for this address, if one was stored less than <see cref="Lifetime"/> ago.
    /// </summary>
    public CachedGoogleCalendarFeed? Get(string icalUrl)
    {
        var key = KeyFor(icalUrl);
        lock (_gate)
        {
            if (_feed is null || _feed.Key != key)
            {
                return null;
            }

            if (_timeProvider.GetUtcNow() - _feed.SyncedAt >= Lifetime)
            {
                _feed = null;
                return null;
            }

            return _feed;
        }
    }

    /// <summary>
    /// Stores a freshly fetched feed, replacing whatever was held.
    /// </summary>
    public CachedGoogleCalendarFeed Store(string icalUrl, Calendar calendar)
    {
        var feed = new CachedGoogleCalendarFeed(KeyFor(icalUrl), calendar, _timeProvider.GetUtcNow());
        lock (_gate)
        {
            _feed = feed;
        }

        return feed;
    }

    // The secret address is not kept in memory longer than it has to be
    private static string KeyFor(string icalUrl) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(icalUrl)));
}

/// <summary>
/// A parsed feed and when it was fetched. Ical.Net evaluates recurrences on shared state, so
/// every read of <see cref="Calendar"/> takes <see cref="Gate"/>.
/// </summary>
public sealed record CachedGoogleCalendarFeed(string Key, Calendar Calendar, DateTimeOffset SyncedAt)
{
    public object Gate { get; } = new();
}

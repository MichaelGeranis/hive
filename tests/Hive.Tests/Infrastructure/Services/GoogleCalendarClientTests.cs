using System.Net;
using System.Text;
using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.Tests.Infrastructure.Services;

/// <summary>
/// Tests for GoogleCalendarClient, against a stubbed HTTP handler. No request leaves the process.
/// </summary>
public class GoogleCalendarClientTests
{
    private const string SecretUrl = "https://calendar.google.com/calendar/ical/em%40acme.com/private-abc123/basic.ics";

    private static readonly DateTimeOffset From = new(2026, 9, 5, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset To = new(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);

    private const string Feed = """
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//Google Inc//Google Calendar 70.9054//EN
        BEGIN:VEVENT
        UID:trip@google.com
        DTSTART;VALUE=DATE:20260901
        DTEND;VALUE=DATE:20260911
        SUMMARY:Team offsite
        LOCATION:Athens
        END:VEVENT
        BEGIN:VEVENT
        UID:weekly@google.com
        DTSTART;TZID=Europe/Athens:20260903T100000
        DTEND;TZID=Europe/Athens:20260903T103000
        RRULE:FREQ=WEEKLY;BYDAY=TH
        EXDATE;TZID=Europe/Athens:20260917T100000
        SUMMARY:Planning
        END:VEVENT
        BEGIN:VEVENT
        UID:weekly@google.com
        RECURRENCE-ID;TZID=Europe/Athens:20260910T100000
        DTSTART;TZID=Europe/Athens:20260910T150000
        DTEND;TZID=Europe/Athens:20260910T153000
        SUMMARY:Planning (moved)
        END:VEVENT
        BEGIN:VEVENT
        UID:gone@google.com
        DTSTART:20260908T090000Z
        DTEND:20260908T100000Z
        STATUS:CANCELLED
        SUMMARY:Cancelled review
        END:VEVENT
        BEGIN:VEVENT
        UID:later@google.com
        DTSTART:20261015T090000Z
        DTEND:20261015T100000Z
        SUMMARY:Out of range
        END:VEVENT
        END:VCALENDAR
        """;

    [Fact]
    public async Task GetEventsAsync_FetchesTheSecretAddressWithoutCredentials()
    {
        var handler = new StubHandler(_ => Ics(Feed));

        await CreateClient(handler).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().Be(SecretUrl);
        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task GetEventsAsync_ExpandsRecurrencesAndDropsCancelledAndOutOfRangeEvents()
    {
        var events = await SyncAsync(CreateClient(new StubHandler(_ => Ics(Feed))));

        events.Select(e => e.Title).Should().Equal("Team offsite", "Planning (moved)", "Planning");
    }

    [Fact]
    public async Task GetEventsAsync_TimedEventIsInUtc()
    {
        var events = await SyncAsync(CreateClient(new StubHandler(_ => Ics(Feed))));

        var planning = events.Single(e => e.Title == "Planning");
        planning.IsAllDay.Should().BeFalse();
        planning.StartsAt.Should().Be(new DateTimeOffset(2026, 9, 24, 7, 0, 0, TimeSpan.Zero));
        planning.EndsAt.Should().Be(new DateTimeOffset(2026, 9, 24, 7, 30, 0, TimeSpan.Zero));
        planning.StartDate.Should().BeNull();
    }

    [Fact]
    public async Task GetEventsAsync_AllDayEventThatStartedBeforeTheRange_EndsOnItsLastDay()
    {
        var events = await SyncAsync(CreateClient(new StubHandler(_ => Ics(Feed))));

        var offsite = events.Single(e => e.Title == "Team offsite");
        offsite.IsAllDay.Should().BeTrue();
        offsite.StartDate.Should().Be(new DateOnly(2026, 9, 1));
        offsite.EndDate.Should().Be(new DateOnly(2026, 9, 10));
        offsite.Location.Should().Be("Athens");
        offsite.StartsAt.Should().BeNull();
    }

    [Fact]
    public async Task GetEventsAsync_GivesEachOccurrenceItsOwnId()
    {
        var events = await SyncAsync(CreateClient(new StubHandler(_ => Ics(Feed))));

        events.Select(e => e.Id).Should().OnlyHaveUniqueItems();
        events.Select(e => e.Id).Should().Contain(new[]
        {
            "trip@google.com@20260901",
            "weekly@google.com@20260924T070000Z"
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "*may have been reset*")]
    [InlineData(HttpStatusCode.Forbidden, "*may have been reset*")]
    [InlineData(HttpStatusCode.TooManyRequests, "*rate limiting*")]
    [InlineData(HttpStatusCode.InternalServerError, "*(500)*")]
    public async Task GetEventsAsync_WhenGoogleRefuses_ThrowsWithoutTheAddress(HttpStatusCode status, string message)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status));

        var act = () => CreateClient(handler).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        var thrown = await act.Should().ThrowAsync<GoogleCalendarException>().WithMessage(message);
        thrown.Which.Message.Should().NotContain("private-abc123");
    }

    [Fact]
    public async Task GetEventsAsync_WhenAnswerIsNotAFeed_Throws()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html><body>Sign in</body></html>", Encoding.UTF8, "text/html")
        });

        var act = () => CreateClient(handler).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        await act.Should().ThrowAsync<GoogleCalendarException>().WithMessage("*not an iCal feed*");
    }

    [Fact]
    public async Task GetEventsAsync_WhenUnreachable_Throws()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("DNS failure"));

        var act = () => CreateClient(handler).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        await act.Should().ThrowAsync<GoogleCalendarException>().WithMessage("*could not be reached*");
    }

    [Fact]
    public async Task GetEventsAsync_CachedOnlyBeforeAnySync_IsNullAndAsksNothing()
    {
        var handler = new StubHandler(_ => Ics(Feed));

        var snapshot = await CreateClient(handler).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOnly);

        snapshot.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsAsync_WithinThirtyMinutesOfASync_IsServedFromMemory()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 9, 24, 9, 0, 0, TimeSpan.Zero));
        var cache = new GoogleCalendarFeedCache(clock);
        var handler = new StubHandler(_ => Ics(Feed));

        var first = await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);
        clock.Advance(TimeSpan.FromMinutes(29));
        var second = await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);
        var cachedOnly = await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOnly);

        handler.Requests.Should().HaveCount(1);
        second!.Events.Should().BeEquivalentTo(first!.Events);
        second.SyncedAt.Should().Be(new DateTimeOffset(2026, 9, 24, 9, 0, 0, TimeSpan.Zero));
        cachedOnly!.Events.Should().HaveCount(first.Events.Count);
    }

    [Fact]
    public async Task GetEventsAsync_AnotherRangeFromTheSameSync_IsExpandedWithoutAskingGoogle()
    {
        var cache = new GoogleCalendarFeedCache(TimeProvider.System);
        var handler = new StubHandler(_ => Ics(Feed));
        await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        var october = await CreateClient(handler, cache).GetEventsAsync(
            SecretUrl,
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            GoogleCalendarRead.CachedOnly);

        october!.Events.Select(e => e.Title).Should().Contain("Out of range");
        handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEventsAsync_AfterThirtyMinutes_FetchesAgain()
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 9, 24, 9, 0, 0, TimeSpan.Zero));
        var cache = new GoogleCalendarFeedCache(clock);
        var handler = new StubHandler(_ => Ics(Feed));
        await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        clock.Advance(TimeSpan.FromMinutes(30));
        var expired = await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOnly);
        var synced = await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        expired.Should().BeNull();
        handler.Requests.Should().HaveCount(2);
        synced!.SyncedAt.Should().Be(new DateTimeOffset(2026, 9, 24, 9, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetEventsAsync_AfterTheAddressChanges_DoesNotServeTheOldCalendar()
    {
        var cache = new GoogleCalendarFeedCache(TimeProvider.System);
        var handler = new StubHandler(_ => Ics(Feed));
        await CreateClient(handler, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);

        var other = await CreateClient(handler, cache).GetEventsAsync(
            "https://calendar.google.com/calendar/ical/other%40acme.com/private-xyz/basic.ics", From, To, GoogleCalendarRead.CachedOnly);

        other.Should().BeNull();
    }

    [Fact]
    public async Task GetEventsAsync_WhenAFetchFails_KeepsNothing()
    {
        var cache = new GoogleCalendarFeedCache(TimeProvider.System);
        var failing = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var act = () => CreateClient(failing, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch);
        await act.Should().ThrowAsync<GoogleCalendarException>();

        (await CreateClient(failing, cache).GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOnly)).Should().BeNull();
    }

    private static async Task<IReadOnlyList<GoogleCalendarEvent>> SyncAsync(GoogleCalendarClient client) =>
        (await client.GetEventsAsync(SecretUrl, From, To, GoogleCalendarRead.CachedOrFetch))!.Events;

    // A fresh cache per client unless one is shared, so tests never see each other's feeds
    private static GoogleCalendarClient CreateClient(StubHandler handler, GoogleCalendarFeedCache? cache = null) =>
        new(new HttpClient(handler), cache ?? new GoogleCalendarFeedCache(TimeProvider.System), NullLogger<GoogleCalendarClient>.Instance);

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now;

        public ManualClock(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private static HttpResponseMessage Ics(string body) => new(HttpStatusCode.OK)
    {
        // iCal lines end in CRLF
        Content = new StringContent(body.Replace("\r\n", "\n").Replace("\n", "\r\n"), Encoding.UTF8, "text/calendar")
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_respond(request));
        }
    }
}

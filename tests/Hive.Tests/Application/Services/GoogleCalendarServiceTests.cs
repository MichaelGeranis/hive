using FluentAssertions;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Exceptions;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for GoogleCalendarService, with the settings repository and feed client mocked.
/// </summary>
public class GoogleCalendarServiceTests
{
    private const string SecretUrl = "https://calendar.google.com/calendar/ical/x/private-abc/basic.ics";
    private static readonly DateTimeOffset SyncedAt = new(2026, 9, 24, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<IAppSettingsRepository> _settingsMock = new();
    private readonly Mock<IGoogleCalendarClient> _clientMock = new();
    private readonly GoogleCalendarService _service;

    public GoogleCalendarServiceTests()
    {
        _service = new GoogleCalendarService(_settingsMock.Object, _clientMock.Object);
    }

    [Fact]
    public async Task GetEventsAsync_WithoutAddress_IsNotConfiguredAndFetchesNothing()
    {
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

        var result = await _service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.IsConfigured.Should().BeFalse();
        result.Events.Should().BeEmpty();
        _clientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetEventsAsync_WithNoSettingsRow_IsNotConfigured()
    {
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AppSettings?)null);

        var result = await _service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAsync_ReadsTheSavedAddressOverAWidenedRange()
    {
        ConfigureAddress();
        _clientMock
            .Setup(c => c.GetEventsAsync(SecretUrl, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<GoogleCalendarRead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Snapshot());

        var result = await _service.SyncAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.IsConfigured.Should().BeTrue();
        result.IsLoaded.Should().BeTrue();
        result.SyncedAt.Should().Be(SyncedAt);
        _clientMock.Verify(c => c.GetEventsAsync(
            SecretUrl,
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero),
            GoogleCalendarRead.CachedOrFetch,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventsAsync_NeverAsksGoogle()
    {
        ConfigureAddress();
        _clientMock
            .Setup(c => c.GetEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<GoogleCalendarRead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Snapshot());

        await _service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        _clientMock.Verify(c => c.GetEventsAsync(
            It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(),
            GoogleCalendarRead.CachedOnly, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEventsAsync_WhenNothingIsHeld_IsConfiguredButNotLoaded()
    {
        ConfigureAddress();
        _clientMock
            .Setup(c => c.GetEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<GoogleCalendarRead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GoogleCalendarSnapshot?)null);

        var result = await _service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.IsConfigured.Should().BeTrue();
        result.IsLoaded.Should().BeFalse();
        result.SyncedAt.Should().BeNull();
        result.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_WithoutAddress_IsNotConfiguredAndFetchesNothing()
    {
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

        var result = await _service.SyncAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.IsConfigured.Should().BeFalse();
        _clientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetEventsAsync_OrdersTimedAndAllDayEventsByStart()
    {
        ConfigureAddress();
        _clientMock
            .Setup(c => c.GetEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<GoogleCalendarRead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Snapshot(
                Timed("b", "Standup", new DateTimeOffset(2026, 9, 3, 9, 0, 0, TimeSpan.Zero)),
                AllDay("c", "Offsite", new DateOnly(2026, 9, 3)),
                Timed("a", "Planning", new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero))));

        var result = await _service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.Events.Select(e => e.Title).Should().Equal("Planning", "Offsite", "Standup");
        result.Events[1].IsAllDay.Should().BeTrue();
        result.Events[1].StartDate.Should().Be(new DateOnly(2026, 9, 3));
    }

    [Fact]
    public async Task GetEventsAsync_LeavesOutAllDayEventsOutsideTheRange()
    {
        ConfigureAddress();
        _clientMock
            .Setup(c => c.GetEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<GoogleCalendarRead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Snapshot(
                AllDay("before", "Last day of August", new DateOnly(2026, 8, 31)),
                new GoogleCalendarEvent("across", "Trip", null, true, null, null, new DateOnly(2026, 8, 30), new DateOnly(2026, 9, 2)),
                AllDay("after", "First of October", new DateOnly(2026, 10, 1))));

        var result = await _service.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        result.Events.Select(e => e.Title).Should().Equal("Trip");
    }

    [Fact]
    public async Task GetEventsAsync_EndBeforeStart_Throws()
    {
        var act = () => _service.GetEventsAsync(new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1));

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*before its start*");
    }

    [Fact]
    public async Task GetEventsAsync_RangeTooLong_Throws()
    {
        var act = () => _service.GetEventsAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*100 days*");
    }

    [Fact]
    public async Task SyncAsync_WhenFeedFails_LetsTheErrorThrough()
    {
        ConfigureAddress();
        _clientMock
            .Setup(c => c.GetEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<GoogleCalendarRead>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GoogleCalendarException("Google Calendar could not be reached."));

        var act = () => _service.SyncAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        await act.Should().ThrowAsync<GoogleCalendarException>();
    }

    private void ConfigureAddress()
    {
        var settings = new AppSettings();
        settings.UpdateGoogleCalendarUrl(SecretUrl);
        _settingsMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
    }

    private static GoogleCalendarSnapshot Snapshot(params GoogleCalendarEvent[] events) => new(events, SyncedAt);

    private static GoogleCalendarEvent Timed(string id, string title, DateTimeOffset start) =>
        new(id, title, null, false, start, start.AddMinutes(30), null, null);

    private static GoogleCalendarEvent AllDay(string id, string title, DateOnly day) =>
        new(id, title, null, true, null, null, day, day);
}

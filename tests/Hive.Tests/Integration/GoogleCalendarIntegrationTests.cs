using Hive.Application.DTOs;
using Hive.Application.Interfaces;

namespace Hive.Tests.Integration;

/// <summary>
/// Google Calendar through the real DI container. Google itself is never called: these tests
/// stop before a secret address is set, which is what guards every request.
/// </summary>
public class GoogleCalendarIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task GetEvents_BeforeAnAddressIsSet_IsNotConfigured()
    {
        // Arrange
        var calendar = GetService<IGoogleCalendarService>();

        // Act
        var result = await calendar.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        result.IsConfigured.Should().BeFalse();
        result.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEvents_WithAnAddressButNoSync_IsNotLoadedAndAsksNobody()
    {
        // Arrange
        await GetService<IAppSettingsService>().UpdateAsync(new UpdateAppSettingsDto
        {
            GoogleCalendarUrl = "https://calendar.google.com/calendar/ical/x/private-abc/basic.ics"
        });
        var calendar = GetService<IGoogleCalendarService>();

        // Act
        var result = await calendar.GetEventsAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // Assert
        result.IsConfigured.Should().BeTrue();
        result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task SavingAnAddress_IsReportedOnlyAsAFlag()
    {
        // Arrange
        var settings = GetService<IAppSettingsService>();

        // Act
        await settings.UpdateAsync(new UpdateAppSettingsDto
        {
            GoogleCalendarUrl = "https://calendar.google.com/calendar/ical/x/private-abc/basic.ics"
        });
        var saved = await settings.GetAsync();

        // Assert
        saved.HasGoogleCalendarUrl.Should().BeTrue();
    }
}

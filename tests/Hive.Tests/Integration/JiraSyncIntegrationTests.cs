using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Core.Exceptions;

namespace Hive.Tests.Integration;

/// <summary>
/// Jira sync through the real DI container. Jira itself is never called: these tests stop
/// at the configuration check, which is what guards every request.
/// </summary>
public class JiraSyncIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task SyncSprintDates_BeforeJiraIsConfigured_ExplainsWhatToSetUp()
    {
        // Arrange
        var settings = GetService<IAppSettingsService>();
        await settings.UpdateAsync(new UpdateAppSettingsDto
        {
            JiraBaseUrl = "https://acme.atlassian.net/browse",
            JiraEmail = "em@acme.com"
        });
        var sync = GetService<IJiraSyncService>();

        // Act
        var act = () => sync.SyncSprintDatesAsync();

        // Assert
        (await act.Should().ThrowAsync<ValidationException>())
            .WithMessage("Jira sync is not configured. Add the Jira API token, Jira board ID in Settings.");
    }
}

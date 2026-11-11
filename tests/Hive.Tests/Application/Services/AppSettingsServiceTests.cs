using System.Text.Json;
using FluentAssertions;
using Hive.Application.DTOs;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Moq;

namespace Hive.Tests.Application.Services;

/// <summary>
/// Tests for AppSettingsService.
/// </summary>
public class AppSettingsServiceTests
{
    private readonly Mock<IAppSettingsRepository> _repositoryMock;
    private readonly AppSettingsService _service;

    public AppSettingsServiceTests()
    {
        _repositoryMock = new Mock<IAppSettingsRepository>();
        _service = new AppSettingsService(_repositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new AppSettingsService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WhenSettingsExist_ReturnsDto()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateTshirtSizeMappings(JsonSerializer.Serialize(new List<TshirtSizeMapping>
        {
            new() { Size = "S", Sprints = 1, Label = "S = 1 sprint" },
            new() { Size = "L", Sprints = 3, Label = "L = 3 sprints" }
        }));

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
        result.TshirtSizeMappings.Should().HaveCount(2);
        result.TshirtSizeMappings[1].Size.Should().Be("L");
        result.TshirtSizeMappings[1].Sprints.Should().Be(3);
    }

    [Fact]
    public async Task GetAsync_WhenSettingsDoNotExist_CreatesDefaultSettings()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings s, CancellationToken _) => s);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.Should().NotBeNull();
        result.TshirtSizeMappings.Should().NotBeEmpty();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_NoSavedSizes_DefaultsUpToAWholeQuarterXxl()
    {
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

        var result = await _service.GetAsync();

        result.TshirtSizeMappings.Select(m => (m.Size, m.Sprints)).Should().Equal(
            ("S", 1m), ("M", 2m), ("L", 4m), ("XL", 6m), ("XXL", 12m));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenSettingsExist_UpdatesAndReturnsDto()
    {
        // Arrange
        var existingEntity = new AppSettings();

        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateAppSettingsDto
        {
            TshirtSizeMappings = new List<TshirtSizeMapping>
            {
                new() { Size = "M", Sprints = 2, Label = "M = 2 sprints" }
            }
        };

        // Act
        var result = await _service.UpdateAsync(dto);

        // Assert
        result.TshirtSizeMappings.Should().ContainSingle()
            .Which.Sprints.Should().Be(2);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenSettingsDoNotExist_CreatesNewSettings()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings s, CancellationToken _) => s);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto());

        // Assert
        result.Should().NotBeNull();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNothingToChange_DoesNotUpdate()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings());

        // Act
        await _service.UpdateAsync(new UpdateAppSettingsDto());

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings());

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateAppSettingsDto
        {
            TshirtSizeMappings = new List<TshirtSizeMapping> { new() { Size = "S", Sprints = 1 } }
        };

        var cts = new CancellationTokenSource();

        // Act
        await _service.UpdateAsync(dto, cts.Token);

        // Assert
        _repositoryMock.Verify(r => r.GetAsync(cts.Token), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AppSettings>(), cts.Token), Times.Once);
    }

    #endregion

    #region Jira settings Tests

    [Fact]
    public async Task UpdateAsync_WithJiraCredentials_StoresThemAndHidesToken()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto
        {
            JiraEmail = "em@acme.com",
            JiraApiToken = "token-123",
            JiraBoardId = 42
        });

        // Assert
        entity.JiraApiToken.Should().Be("token-123");
        result.JiraEmail.Should().Be("em@acme.com");
        result.HasJiraApiToken.Should().BeTrue();
        result.JiraBoardId.Should().Be(42);
        typeof(AppSettingsDto).GetProperty("JiraApiToken").Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithSentimentProviderAndOllama_StoresAndReturnsThem()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto
        {
            SentimentProvider = "Claude",
            OllamaBaseUrl = "http://ollama.local:11434",
            OllamaModel = "mistral"
        });

        // Assert
        entity.SentimentProvider.Should().Be(SentimentProvider.Claude);
        result.SentimentProvider.Should().Be("Claude");
        result.OllamaBaseUrl.Should().Be("http://ollama.local:11434");
        result.OllamaModel.Should().Be("mistral");
    }

    [Fact]
    public async Task UpdateAsync_WithRetroSummarySettings_StoresAndReturnsThem()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { RetroSummaryEnabled = true, RetroSummaryCount = 10 });

        // Assert
        entity.RetroSummaryEnabled.Should().BeTrue();
        entity.RetroSummaryCount.Should().Be(10);
        result.RetroSummaryEnabled.Should().BeTrue();
        result.RetroSummaryCount.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_WithTaskReviewEnabled_StoresAndReturnsIt()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { TaskReviewEnabled = true });

        // Assert
        entity.TaskReviewEnabled.Should().BeTrue();
        result.TaskReviewEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyRetroSummaryEnabled_KeepsTheCount()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateRetroSummarySettings(false, 12);
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        await _service.UpdateAsync(new UpdateAppSettingsDto { RetroSummaryEnabled = true });

        // Assert
        entity.RetroSummaryEnabled.Should().BeTrue();
        entity.RetroSummaryCount.Should().Be(12);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyOllamaModel_KeepsTheAddress()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateOllamaConnection("http://ollama.local:11434", "llama3.1:8b");
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        await _service.UpdateAsync(new UpdateAppSettingsDto { OllamaModel = "qwen2.5:7b" });

        // Assert
        entity.OllamaBaseUrl.Should().Be("http://ollama.local:11434");
        entity.OllamaModel.Should().Be("qwen2.5:7b");
    }

    [Fact]
    public async Task UpdateAsync_WithGoogleCalendarUrl_StoresItButOnlyExposesAFlag()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto
        {
            GoogleCalendarUrl = "https://calendar.google.com/calendar/ical/x/private-abc/basic.ics"
        });

        // Assert
        entity.GoogleCalendarUrl.Should().Be("https://calendar.google.com/calendar/ical/x/private-abc/basic.ics");
        result.HasGoogleCalendarUrl.Should().BeTrue();
        typeof(AppSettingsDto).GetProperty("GoogleCalendarUrl").Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithMinMemberProducts_StoresAndReturnsIt()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { MinMemberProducts = 3 });

        // Assert
        entity.MinMemberProducts.Should().Be(3);
        result.MinMemberProducts.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_WithStaleTaskDays_StoresAndReturnsIt()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { StaleTaskDays = 8 });

        // Assert
        entity.StaleTaskDays.Should().Be(8);
        result.StaleTaskDays.Should().Be(8);
    }

    [Fact]
    public async Task UpdateAsync_WithOneOnOneCadence_StoresAndReturnsIt()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { OneOnOneCadenceDays = 21 });

        // Assert
        entity.OneOnOneCadenceDays.Should().Be(21);
        result.OneOnOneCadenceDays.Should().Be(21);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyGoogleCalendarUrl_ClearsIt()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateGoogleCalendarUrl("https://calendar.google.com/calendar/ical/x/private-abc/basic.ics");
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { GoogleCalendarUrl = "" });

        // Assert
        entity.GoogleCalendarUrl.Should().BeNull();
        result.HasGoogleCalendarUrl.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithoutGoogleCalendarUrl_LeavesItUnchanged()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateGoogleCalendarUrl("https://calendar.google.com/calendar/ical/x/private-abc/basic.ics");
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        await _service.UpdateAsync(new UpdateAppSettingsDto { MaxBlockedTasks = 3 });

        // Assert
        entity.HasGoogleCalendarUrl.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithoutJiraFields_LeavesJiraSettingsUnchanged()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateJiraEmail("em@acme.com");
        entity.UpdateJiraApiToken("token-123");
        entity.UpdateJiraBoardId(42);
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        await _service.UpdateAsync(new UpdateAppSettingsDto { MaxBlockedTasks = 3 });

        // Assert
        entity.JiraEmail.Should().Be("em@acme.com");
        entity.JiraApiToken.Should().Be("token-123");
        entity.JiraBoardId.Should().Be(42);
    }

    [Fact]
    public async Task UpdateAsync_WithClearJiraBoardId_ClearsBoard()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateJiraBoardId(42);
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto { ClearJiraBoardId = true });

        // Assert
        result.JiraBoardId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidJiraBoardId_Throws()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

        // Act
        var act = () => _service.UpdateAsync(new UpdateAppSettingsDto { JiraBoardId = 0 });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    [Fact]
    public async Task GetAsync_ReturnsTheBrowseUrlForIssueLinks()
    {
        // Arrange
        var entity = new AppSettings();
        entity.UpdateJiraBaseUrl("https://example.atlassian.net");
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.GetAsync();

        // Assert
        result.JiraBaseUrl.Should().Be("https://example.atlassian.net");
        result.JiraBrowseUrl.Should().Be("https://example.atlassian.net/browse");
    }

    [Fact]
    public async Task UpdateAsync_WithWorkCategoryEpicNames_StoresAndReturnsThem()
    {
        // Arrange
        var entity = new AppSettings();
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        // Act
        var result = await _service.UpdateAsync(new UpdateAppSettingsDto
        {
            WorkCategoryEpicNames = new() { ["TechMaintenance"] = ["KTLO"], ["TechDebt"] = ["Tech Debt"] }
        });

        // Assert
        entity.GetWorkCategoryEpicNames()[WorkCategory.TechMaintenance].Should().Equal("KTLO");
        result.WorkCategoryEpicNames["TechMaintenance"].Should().Equal("KTLO");
        result.WorkCategoryEpicNames["TechRoadmap"].Should().BeEmpty();
        result.WorkCategoryEpicNames.Should().NotContainKey("Productisation");
    }

    [Theory]
    [InlineData("Spaceships")]
    [InlineData("3")]
    public async Task UpdateAsync_WithAnUnknownWorkCategory_Throws(string category)
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new AppSettings());

        // Act
        var act = () => _service.UpdateAsync(new UpdateAppSettingsDto { WorkCategoryEpicNames = new() { [category] = ["x"] } });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage($"*{category}*");
    }
}

using FluentAssertions;
using Hive.Core.Entities;

namespace Hive.Tests.Core.Entities;

public class AppSettingsTests
{
    [Fact]
    public void Constructor_CreatesAppSettingsWithDefaults()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.Id.Should().NotBeEmpty();
        settings.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        settings.UpdatedAt.Should().BeNull();
    }

    #region Sentiment provider

    [Fact]
    public void Constructor_DefaultsToLocalOllama()
    {
        var settings = new AppSettings();

        settings.SentimentProvider.Should().Be(SentimentProvider.Ollama);
        settings.OllamaBaseUrl.Should().Be("http://localhost:11434");
        settings.OllamaModel.Should().Be("llama3.1:8b");
        settings.IsSentimentProviderConfigured.Should().BeTrue();
    }

    [Theory]
    [InlineData("Claude", SentimentProvider.Claude)]
    [InlineData("ollama", SentimentProvider.Ollama)]
    [InlineData(" claude ", SentimentProvider.Claude)]
    public void UpdateSentimentProvider_WithKnownProvider_Sets(string input, SentimentProvider expected)
    {
        var settings = new AppSettings();

        settings.UpdateSentimentProvider(input);

        settings.SentimentProvider.Should().Be(expected);
    }

    [Theory]
    [InlineData("OpenAI")]
    [InlineData("")]
    [InlineData("7")]
    public void UpdateSentimentProvider_WithUnknownProvider_Throws(string input)
    {
        var act = () => new AppSettings().UpdateSentimentProvider(input);

        act.Should().Throw<ArgumentException>().WithMessage("*Ollama or Claude*");
    }

    [Fact]
    public void IsSentimentProviderConfigured_WithClaudeAndNoKey_IsFalse()
    {
        var settings = new AppSettings();
        settings.UpdateSentimentProvider("Claude");

        settings.IsSentimentProviderConfigured.Should().BeFalse();

        settings.UpdateClaudeApiKey("sk-test");
        settings.IsSentimentProviderConfigured.Should().BeTrue();
    }

    [Fact]
    public void UpdateOllamaConnection_TrimsTheAddressAndModel()
    {
        var settings = new AppSettings();

        settings.UpdateOllamaConnection(" http://192.168.1.20:11434/ ", " mistral:7b ");

        settings.OllamaBaseUrl.Should().Be("http://192.168.1.20:11434");
        settings.OllamaModel.Should().Be("mistral:7b");
    }

    [Fact]
    public void UpdateOllamaConnection_WithBlanks_GoesBackToTheDefaults()
    {
        var settings = new AppSettings();
        settings.UpdateOllamaConnection("http://other:11434", "mistral");

        settings.UpdateOllamaConnection("", null);

        settings.OllamaBaseUrl.Should().Be(AppSettings.DefaultOllamaBaseUrl);
        settings.OllamaModel.Should().Be(AppSettings.DefaultOllamaModel);
    }

    [Theory]
    [InlineData("localhost:11434")]
    [InlineData("ftp://localhost:11434")]
    [InlineData("not a url")]
    public void UpdateOllamaConnection_WithBadAddress_Throws(string url)
    {
        var act = () => new AppSettings().UpdateOllamaConnection(url, "llama3.1:8b");

        act.Should().Throw<ArgumentException>().WithMessage("*http://*");
    }

    [Fact]
    public void UpdateOllamaConnection_WithSpaceInModel_Throws()
    {
        var act = () => new AppSettings().UpdateOllamaConnection("http://localhost:11434", "llama 3");

        act.Should().Throw<ArgumentException>().WithMessage("*without spaces*");
    }

    #endregion

    #region Jira sync settings

    [Theory]
    [InlineData("https://acme.atlassian.net/browse", "https://acme.atlassian.net")]
    [InlineData("https://acme.atlassian.net/browse/", "https://acme.atlassian.net")]
    [InlineData("https://acme.atlassian.net", "https://acme.atlassian.net")]
    public void GetJiraSiteUrl_WithBaseUrl_ReturnsSiteRoot(string baseUrl, string expected)
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl(baseUrl);

        settings.GetJiraSiteUrl().Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("acme.atlassian.net/browse")]
    [InlineData("ftp://acme.atlassian.net")]
    public void GetJiraSiteUrl_WithoutUsableBaseUrl_ReturnsNull(string? baseUrl)
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl(baseUrl);

        settings.GetJiraSiteUrl().Should().BeNull();
    }

    [Fact]
    public void UpdateJiraEmail_WithValidEmail_TrimsAndStores()
    {
        var settings = new AppSettings();

        settings.UpdateJiraEmail("  em@acme.com ");

        settings.JiraEmail.Should().Be("em@acme.com");
        settings.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@acme.com")]
    [InlineData("em@")]
    [InlineData("e m@acme.com")]
    public void UpdateJiraEmail_WithInvalidEmail_Throws(string email)
    {
        var settings = new AppSettings();

        var act = () => settings.UpdateJiraEmail(email);

        act.Should().Throw<ArgumentException>().WithMessage("*valid email*");
    }

    [Fact]
    public void UpdateJiraEmail_WithBlank_Clears()
    {
        var settings = new AppSettings();
        settings.UpdateJiraEmail("em@acme.com");

        settings.UpdateJiraEmail("  ");

        settings.JiraEmail.Should().BeNull();
    }

    [Fact]
    public void UpdateJiraApiToken_WithToken_SetsFlag()
    {
        var settings = new AppSettings();

        settings.UpdateJiraApiToken(" token-123 ");

        settings.JiraApiToken.Should().Be("token-123");
        settings.HasJiraApiToken.Should().BeTrue();
    }

    [Fact]
    public void UpdateJiraApiToken_WithBlank_ClearsToken()
    {
        var settings = new AppSettings();
        settings.UpdateJiraApiToken("token-123");

        settings.UpdateJiraApiToken("");

        settings.JiraApiToken.Should().BeNull();
        settings.HasJiraApiToken.Should().BeFalse();
    }

    [Fact]
    public void UpdateJiraApiToken_TooLong_Throws()
    {
        var settings = new AppSettings();

        var act = () => settings.UpdateJiraApiToken(new string('x', 501));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void UpdateJiraBoardId_NotPositive_Throws(int boardId)
    {
        var settings = new AppSettings();

        var act = () => settings.UpdateJiraBoardId(boardId);

        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void UpdateJiraBoardId_WithNull_Clears()
    {
        var settings = new AppSettings();
        settings.UpdateJiraBoardId(42);

        settings.UpdateJiraBoardId(null);

        settings.JiraBoardId.Should().BeNull();
    }

    [Fact]
    public void GetMissingJiraSprintSyncSettings_WhenNothingSet_ListsEverything()
    {
        var settings = new AppSettings();

        settings.GetMissingJiraSprintSyncSettings().Should().Equal(
            "Jira base URL", "Jira email", "Jira API token", "Jira board ID");
    }

    [Fact]
    public void GetMissingJiraSprintSyncSettings_WhenFullyConfigured_IsEmpty()
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl("https://acme.atlassian.net/browse");
        settings.UpdateJiraEmail("em@acme.com");
        settings.UpdateJiraApiToken("token");
        settings.UpdateJiraBoardId(42);

        settings.GetMissingJiraSprintSyncSettings().Should().BeEmpty();
    }

    #endregion

    [Theory]
    [InlineData("MAR", "MAR")]
    [InlineData(" mar ", "MAR")]
    [InlineData("OPS2", "OPS2")]
    public void UpdateJiraProjectKey_NormalisesToUpperCase(string input, string expected)
    {
        var settings = new AppSettings();

        settings.UpdateJiraProjectKey(input);

        settings.JiraProjectKey.Should().Be(expected);
    }

    [Theory]
    [InlineData("2MAR")]
    [InlineData("MAR-1")]
    [InlineData("MA R")]
    public void UpdateJiraProjectKey_WithInvalidKey_Throws(string key)
    {
        var act = () => new AppSettings().UpdateJiraProjectKey(key);

        act.Should().Throw<ArgumentException>().WithMessage("*space key*");
    }

    [Fact]
    public void UpdateGoogleCalendarUrl_WithSecretAddress_StoresItTrimmed()
    {
        var settings = new AppSettings();

        settings.UpdateGoogleCalendarUrl("  https://calendar.google.com/calendar/ical/em%40acme.com/private-abc/basic.ics ");

        settings.GoogleCalendarUrl.Should().Be("https://calendar.google.com/calendar/ical/em%40acme.com/private-abc/basic.ics");
        settings.HasGoogleCalendarUrl.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateGoogleCalendarUrl_WithBlank_ClearsIt(string? url)
    {
        var settings = new AppSettings();
        settings.UpdateGoogleCalendarUrl("https://calendar.google.com/calendar/ical/x/private-abc/basic.ics");

        settings.UpdateGoogleCalendarUrl(url);

        settings.GoogleCalendarUrl.Should().BeNull();
        settings.HasGoogleCalendarUrl.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://calendar.google.com/calendar/ical/x/private-abc/basic.ics")]
    [InlineData("webcal://calendar.google.com/calendar/ical/x/private-abc/basic.ics")]
    [InlineData("calendar.google.com/calendar/ical/x/private-abc/basic.ics")]
    public void UpdateGoogleCalendarUrl_WithoutHttps_Throws(string url)
    {
        var act = () => new AppSettings().UpdateGoogleCalendarUrl(url);

        act.Should().Throw<ArgumentException>().WithMessage("*https://*");
    }

    [Fact]
    public void UpdateGoogleCalendarUrl_TooLong_Throws()
    {
        var act = () => new AppSettings().UpdateGoogleCalendarUrl("https://calendar.google.com/" + new string('a', 2000));

        act.Should().Throw<ArgumentException>().WithMessage("*2000*");
    }

    [Fact]
    public void GetMissingJiraParentSyncSettings_NeedsTheSpaceKeyButNotTheBoard()
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl("https://acme.atlassian.net/browse");
        settings.UpdateJiraEmail("em@acme.com");
        settings.UpdateJiraApiToken("token");

        settings.GetMissingJiraParentSyncSettings().Should().Equal("Jira space key");

        settings.UpdateJiraProjectKey("MAR");
        settings.GetMissingJiraParentSyncSettings().Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://example.atlassian.net")]
    [InlineData("https://example.atlassian.net/")]
    [InlineData("https://example.atlassian.net/browse")]
    [InlineData("https://example.atlassian.net/browse/")]
    public void GetJiraBrowseUrl_WithOrWithoutBrowsePath_PointsAtBrowse(string baseUrl)
    {
        var settings = new AppSettings();
        settings.UpdateJiraBaseUrl(baseUrl);

        settings.GetJiraBrowseUrl().Should().Be("https://example.atlassian.net/browse");
    }

    [Fact]
    public void GetJiraBrowseUrl_WithoutBaseUrl_IsNull()
    {
        new AppSettings().GetJiraBrowseUrl().Should().BeNull();
    }

    #region 1:1 cadence

    [Fact]
    public void OneOnOneCadenceDays_DefaultsToTwoWeeks()
    {
        new AppSettings().OneOnOneCadenceDays.Should().Be(14);
    }

    [Fact]
    public void UpdateOneOnOneCadence_WithinRange_SetsIt()
    {
        var settings = new AppSettings();

        settings.UpdateOneOnOneCadence(7);

        settings.OneOnOneCadenceDays.Should().Be(7);
        settings.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MinMemberProducts_DefaultsToTwo()
    {
        new AppSettings().MinMemberProducts.Should().Be(AppSettings.DefaultMinMemberProducts).And.Be(2);
    }

    [Fact]
    public void UpdateMinMemberProducts_WithinRange_SetsIt()
    {
        var settings = new AppSettings();

        settings.UpdateMinMemberProducts(4);

        settings.MinMemberProducts.Should().Be(4);
        settings.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void UpdateMinMemberProducts_OutOfRange_Throws(int minMemberProducts)
    {
        var act = () => new AppSettings().UpdateMinMemberProducts(minMemberProducts);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RetroSummary_IsOffAndReadsSixRetros_ByDefault()
    {
        var settings = new AppSettings();

        settings.RetroSummaryEnabled.Should().BeFalse();
        settings.RetroSummaryCount.Should().Be(6);
    }

    [Fact]
    public void UpdateRetroSummarySettings_WithinRange_SetsThem()
    {
        var settings = new AppSettings();

        settings.UpdateRetroSummarySettings(true, 10);

        settings.RetroSummaryEnabled.Should().BeTrue();
        settings.RetroSummaryCount.Should().Be(10);
        settings.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void UpdateRetroSummarySettings_CountOutOfRange_Throws(int count)
    {
        var act = () => new AppSettings().UpdateRetroSummarySettings(true, count);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TaskReview_IsOffByDefault_AndCanBeTurnedOn()
    {
        var settings = new AppSettings();
        settings.TaskReviewEnabled.Should().BeFalse();

        settings.UpdateTaskReviewEnabled(true);

        settings.TaskReviewEnabled.Should().BeTrue();
        settings.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void StaleTaskDays_DefaultsToFive()
    {
        new AppSettings().StaleTaskDays.Should().Be(TeamTask.DefaultStaleAfterDays).And.Be(5);
    }

    [Fact]
    public void UpdateStaleTaskDays_WithinRange_SetsIt()
    {
        var settings = new AppSettings();

        settings.UpdateStaleTaskDays(10);

        settings.StaleTaskDays.Should().Be(10);
        settings.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void UpdateStaleTaskDays_OutOfRange_Throws(int days)
    {
        var act = () => new AppSettings().UpdateStaleTaskDays(days);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void UpdateOneOnOneCadence_OutOfRange_Throws(int days)
    {
        var settings = new AppSettings();

        var act = () => settings.UpdateOneOnOneCadence(days);

        act.Should().Throw<ArgumentException>();
        settings.OneOnOneCadenceDays.Should().Be(14);
    }

    #endregion

    [Fact]
    public void GetWorkCategoryEpicNames_WhenNeverSet_IsTheDefaults()
    {
        new AppSettings().GetWorkCategoryEpicNames().Should().BeEquivalentTo(WorkCategories.DefaultEpicNames);
    }

    [Fact]
    public void UpdateWorkCategoryEpicNames_StoresTrimmedNames_AndCategoriesLeftOutHaveNone()
    {
        var settings = new AppSettings();

        settings.UpdateWorkCategoryEpicNames(new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechDebt] = ["  Tech Debt ", "Refactoring"],
            [WorkCategory.TechMaintenance] = ["KTLO"]
        });

        var names = settings.GetWorkCategoryEpicNames();
        names[WorkCategory.TechDebt].Should().Equal("Tech Debt", "Refactoring");
        names[WorkCategory.TechMaintenance].Should().Equal("KTLO");
        names[WorkCategory.TechRoadmap].Should().BeEmpty();
        names.Should().NotContainKey(WorkCategory.Productisation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void UpdateWorkCategoryEpicNames_WithANameWithoutLettersOrDigits_Throws(string name)
    {
        var act = () => new AppSettings().UpdateWorkCategoryEpicNames(
            new Dictionary<WorkCategory, IReadOnlyList<string>> { [WorkCategory.TechDebt] = [name] });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateWorkCategoryEpicNames_WithOneNameInTwoCategories_Throws()
    {
        var act = () => new AppSettings().UpdateWorkCategoryEpicNames(new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechDebt] = ["Tech Debt"],
            [WorkCategory.TechRoadmap] = ["TECH-DEBT"]
        });

        act.Should().Throw<ArgumentException>().WithMessage("*names both*");
    }

    [Fact]
    public void UpdateWorkCategoryEpicNames_ForProductisation_Throws()
    {
        var act = () => new AppSettings().UpdateWorkCategoryEpicNames(
            new Dictionary<WorkCategory, IReadOnlyList<string>> { [WorkCategory.Productisation] = ["Features"] });

        act.Should().Throw<ArgumentException>().WithMessage("*Productisation*");
    }

    [Fact]
    public void UpdateWorkCategoryEpicNames_WithATooLongName_Throws()
    {
        var act = () => new AppSettings().UpdateWorkCategoryEpicNames(new Dictionary<WorkCategory, IReadOnlyList<string>>
        {
            [WorkCategory.TechDebt] = [new string('x', AppSettings.MaxEpicNameLength + 1)]
        });

        act.Should().Throw<ArgumentException>();
    }
}

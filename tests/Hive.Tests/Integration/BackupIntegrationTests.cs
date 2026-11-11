using System.Text.Json;
using Hive.Application;
using Hive.Application.DTOs;
using Hive.Application.Interfaces;
using Hive.Application.Services;
using Hive.Core.Entities;
using Hive.Core.Interfaces;
using Hive.Infrastructure;
using Hive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Tests.Integration;

/// <summary>
/// A backup taken from one Hive and restored into an empty one brings back every table and
/// every stored field, settings included and secrets excluded.
/// </summary>
public class BackupIntegrationTests : IntegrationTestBase
{
    public BackupIntegrationTests() : base(seedData: true)
    {
    }

    [Fact]
    public void Backup_CoversEveryTableInTheDatabase()
    {
        var options = new DbContextOptionsBuilder<HiveDbContext>().UseSqlite("DataSource=:memory:").Options;
        using var context = new HiveDbContext(options);
        var databaseTables = context.Model.GetEntityTypes().Select(e => e.ClrType.Name);

        var backupTables = ((BackupService)GetService<IBackupService>()).TableNames.Append(BackupService.SettingsTable);

        backupTables.Should().BeEquivalentTo(databaseTables,
            "every table must be in the backup; add a new one to BackupService's table list");
    }

    [Fact]
    public async Task ExportThenImport_IntoAnEmptyHive_RestoresEveryStoredField()
    {
        // Arrange — the seeded team, tasks, 1:1s and leave, plus planning, career plans,
        // products, parents, sprints and settings, with state only the entities' methods set
        await AddMoreThanTheSeedAsync();
        var source = await GetService<IBackupService>().ExportAsync();

        // Through JSON as the Settings page sends it, into a Hive with nothing in it
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var file = JsonSerializer.Deserialize<BackupDto>(JsonSerializer.Serialize(source, web), web)!;
        using var target = CreateEmptyHive();
        var targetBackup = target.GetRequiredService<IBackupService>();

        // Act
        var result = await targetBackup.ImportAsync(file);

        // Assert
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
        result.SettingsRestored.Should().BeTrue();
        var restored = await targetBackup.ExportAsync();
        foreach (var (table, rows) in source.Tables.Where(t => t.Key != BackupService.SettingsTable))
        {
            restored.Tables[table].Select(r => r.GetRawText())
                .Should().BeEquivalentTo(rows.Select(r => r.GetRawText()), $"every {table} row comes back exactly");
        }

        var settings = await target.GetRequiredService<IAppSettingsRepository>().GetAsync();
        settings!.OneOnOneCadenceDays.Should().Be(21);
        settings.ClaudeApiKey.Should().BeNull("secrets are never in a backup");
        MustHaveRows.Should().OnlyContain(table => source.Tables[table].Count > 0, "the round trip must not be vacuous");
    }

    [Fact]
    public async Task ExportThenImport_IntoAnEmptySqliteDatabase_RestoresEveryStoredField()
    {
        // Arrange — the same backup, restored into a real, migrated SQLite database
        await AddMoreThanTheSeedAsync();
        var source = await GetService<IBackupService>().ExportAsync();
        var databasePath = Path.Combine(Path.GetTempPath(), $"hive-backup-test-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSqliteInfrastructureServices($"Data Source={databasePath};Pooling=False", seedData: false);
        services.AddApplicationServices();

        try
        {
            using var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<HiveDbContext>().Database.MigrateAsync();
            }

            // Act
            RestoreResultDto result;
            using (var scope = provider.CreateScope())
            {
                result = await scope.ServiceProvider.GetRequiredService<IBackupService>().ImportAsync(source);
            }

            // Assert — read back through a fresh scope, so nothing comes from EF's change tracker
            result.Errors.Should().BeEmpty();
            using (var scope = provider.CreateScope())
            {
                var restored = await scope.ServiceProvider.GetRequiredService<IBackupService>().ExportAsync();
                foreach (var (table, rows) in source.Tables.Where(t => t.Key != BackupService.SettingsTable))
                {
                    restored.Tables[table].Select(r => WithoutUtcMarker(r.GetRawText()))
                        .Should().BeEquivalentTo(rows.Select(r => WithoutUtcMarker(r.GetRawText())), $"every {table} row comes back exactly");
                }

                var settings = await scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>().GetAsync();
                settings!.OneOnOneCadenceDays.Should().Be(21);
                settings.ClaudeApiKey.Should().BeNull();
            }
        }
        finally
        {
            foreach (var file in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task Import_OfItsOwnBackup_KeepsEverythingAsItIs()
    {
        // Arrange
        var backupService = GetService<IBackupService>();
        var backup = await backupService.ExportAsync();

        // Act
        var result = await backupService.ImportAsync(backup);

        // Assert
        result.Success.Should().BeTrue();
        result.Restored.Should().BeEmpty();
        result.Skipped.Values.Sum().Should().Be(backup.Tables.Where(t => t.Key != BackupService.SettingsTable).Sum(t => t.Value.Count));
    }

    // Tables the arranged data fills, so the round trip is not vacuous for them
    private static readonly HashSet<string> MustHaveRows =
    [
        nameof(DirectReport), nameof(Project), nameof(TeamTask), nameof(OneOnOneMeeting), nameof(Leave),
        nameof(Product), nameof(Parent), nameof(Sprint), nameof(Quarter), nameof(Initiative),
        nameof(SkillRoadmap), nameof(RoadmapSkill), nameof(CareerPlan), nameof(CareerPlanSkill)
    ];

    private async Task AddMoreThanTheSeedAsync()
    {
        var people = await GetService<IDirectReportRepository>().GetAllAsync();

        var tasks = GetService<ITeamTaskRepository>();
        var task = (await tasks.GetAllAsync())[0];
        task.Start();
        task.Complete();
        await tasks.UpdateAsync(task);

        var sprint = new Sprint("LP_4Q26_S1");
        sprint.UpdateDates(new DateTime(2026, 10, 5), new DateTime(2026, 10, 16));
        await GetService<ISprintRepository>().AddAsync(sprint);

        await GetService<IProductService>().CreateAsync(new CreateProductDto { Name = "Matching", Description = "Matches rights" });
        await GetService<IParentRepository>().AddAsync(Parent.FromJira("MAR-1", "Epic", "Tech Debt", ["Matching"]));

        // A new quarter comes with its Maintenance lane, whose work type is 0; and a mix with a
        // 0% share. Both are the value a SQLite column default would otherwise replace.
        var quarter = await GetService<IQuarterlyPlanningService>().CreateQuarterAsync(new CreateQuarterDto { Year = 2026, QuarterNumber = 4 });
        await GetService<IQuarterlyPlanningService>().CreateInitiativeAsync(new CreateInitiativeDto { QuarterId = quarter.Id, Name = "Rights search" });
        var quarters = GetService<IQuarterRepository>();
        var storedQuarter = (await quarters.GetByIdAsync(quarter.Id))!;
        storedQuarter.SetMixTarget(70, 0, 30);
        await quarters.UpdateAsync(storedQuarter);

        var roadmap = await GetService<ISkillRoadmapService>().AddFromLibraryAsync("backend");
        await GetService<ICareerPlanService>().CreateAsync(new CreateCareerPlanDto
        {
            DirectReportId = people[0].Id,
            RoadmapId = roadmap.Id,
            CurrentStage = "Mid"
        });

        var settingsRepository = GetService<IAppSettingsRepository>();
        var settings = await settingsRepository.GetAsync();
        if (settings is null)
        {
            settings = new AppSettings();
            await settingsRepository.AddAsync(settings);
        }
        settings.UpdateOneOnOneCadence(21);
        settings.UpdateClaudeApiKey("sk-secret");
        await settingsRepository.UpdateAsync(settings);
    }

    // SQLite keeps a date's value but not its DateTimeKind, for every row it stores, so a UTC
    // date written in memory ("...Z") reads back without the marker
    private static string WithoutUtcMarker(string json) =>
        System.Text.RegularExpressions.Regex.Replace(json, "(\\d)Z\"", "$1\"");

    private static ServiceProvider CreateEmptyHive()
    {
        var services = new ServiceCollection();
        services.AddInfrastructureServices(seedData: false);
        services.AddApplicationServices();
        return services.BuildServiceProvider();
    }
}

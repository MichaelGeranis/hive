using Hive.Application.Interfaces;
using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using Hive.Infrastructure.Persistence.Repositories.Sqlite;
using Hive.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hive.Infrastructure;

/// <summary>
/// Extension methods for configuring Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services using in-memory storage (for testing).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, bool seedData = true)
    {
        // Register InMemoryDbContext as Singleton (simulates database)
        services.AddSingleton<InMemoryDbContext>(sp =>
        {
            var context = new InMemoryDbContext();
            if (seedData)
            {
                context.SeedData();
            }
            return context;
        });

        // Nothing to back up: the in-memory database has no file
        services.AddSingleton<IAutomaticBackups, NoDatabaseBackups>();

        // Register in-memory repositories
        services.AddScoped<IDirectReportRepository, DirectReportRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, OneOnOneMeetingRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITeamTaskRepository, TeamTaskRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IManagerNoteRepository, ManagerNoteRepository>();
        services.AddScoped<INoteFolderRepository, NoteFolderRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintCapacityRepository, SprintCapacityRepository>();
        services.AddScoped<IParentRepository, ParentRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IProjectKnowledgeRepository, ProjectKnowledgeRepository>();
        services.AddScoped<ISentimentAnalysisCacheRepository, SentimentAnalysisCacheRepository>();
        services.AddScoped<IRetroSummaryRepository, RetroSummaryRepository>();
        services.AddScoped<ITaskReadinessReviewRepository, TaskReadinessReviewRepository>();
        services.AddScoped<IQuarterRepository, QuarterRepository>();
        services.AddScoped<IInitiativeRepository, InitiativeRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ISprintGoalRepository, SprintGoalRepository>();
        services.AddScoped<IInitiativeDependencyRepository, InitiativeDependencyRepository>();
        services.AddScoped<IInitiativeMemberRepository, InitiativeMemberRepository>();
        services.AddScoped<IExternalDependencyRepository, ExternalDependencyRepository>();
        services.AddScoped<IQuarterMemberRepository, QuarterMemberRepository>();
        services.AddScoped<IInitiativeProductRepository, InitiativeProductRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProjectProductRepository, ProjectProductRepository>();
        services.AddScoped<ISkillRoadmapRepository, SkillRoadmapRepository>();
        services.AddScoped<IRoadmapSkillRepository, RoadmapSkillRepository>();
        services.AddScoped<ICareerPlanRepository, CareerPlanRepository>();
        services.AddScoped<ICareerPlanSkillRepository, CareerPlanSkillRepository>();

        // Register Claude API service with HttpClient. The Anthropic SDK applies its own
        // per-request timeout and retries, so the HttpClient must not cut requests off first
        services.AddHttpClient<IClaudeApiService, ClaudeApiService>(client => client.Timeout = Timeout.InfiniteTimeSpan);

        // Register the Ollama client with HttpClient. A local model reading months of notes on
        // a laptop can take minutes, so the timeout is generous
        services.AddHttpClient<IOllamaApiService, OllamaApiService>(client => client.Timeout = TimeSpan.FromMinutes(10));

        // Register the read-only Jira Cloud client with HttpClient
        services.AddHttpClient<IJiraApiClient, JiraApiClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

        // Register the read-only Google Calendar (iCal feed) client with HttpClient. The feed's
        // address is its secret, so the factory's request logging, which logs every URL, is removed.
        // The fetched feed is held in memory for 30 minutes, shared across requests
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<GoogleCalendarFeedCache>();
        services.AddHttpClient<IGoogleCalendarClient, GoogleCalendarClient>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services using SQLite storage (for production).
    /// </summary>
    /// <param name="databaseKey">
    /// The key that encrypts the database and its backups (<see cref="DatabaseEncryption"/>), or
    /// null to keep them unencrypted.
    /// </param>
    public static IServiceCollection AddSqliteInfrastructureServices(
        this IServiceCollection services,
        string connectionString,
        bool seedData = true,
        string? databaseKey = null)
    {
        // Register HiveDbContext with SQLite
        var keyedConnectionString = DatabaseEncryption.WithKey(connectionString, databaseKey);
        services.AddDbContext<HiveDbContext>(options =>
            options.UseSqlite(keyedConnectionString));

        // Register SQLite repositories
        services.AddScoped<IDirectReportRepository, SqliteDirectReportRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, SqliteOneOnOneMeetingRepository>();
        services.AddScoped<IProjectRepository, SqliteProjectRepository>();
        services.AddScoped<ITeamTaskRepository, SqliteTeamTaskRepository>();
        services.AddScoped<IAppSettingsRepository, SqliteAppSettingsRepository>();
        services.AddScoped<ILeaveRepository, SqliteLeaveRepository>();
        services.AddScoped<IManagerNoteRepository, SqliteManagerNoteRepository>();
        services.AddScoped<INoteFolderRepository, SqliteNoteFolderRepository>();
        services.AddScoped<ISprintRepository, SqliteSprintRepository>();
        services.AddScoped<ISprintCapacityRepository, SqliteSprintCapacityRepository>();
        services.AddScoped<IParentRepository, SqliteParentRepository>();
        services.AddScoped<IActivityRepository, SqliteActivityRepository>();
        services.AddScoped<IProjectKnowledgeRepository, SqliteProjectKnowledgeRepository>();
        services.AddScoped<ISentimentAnalysisCacheRepository, SqliteSentimentAnalysisCacheRepository>();
        services.AddScoped<IRetroSummaryRepository, SqliteRetroSummaryRepository>();
        services.AddScoped<ITaskReadinessReviewRepository, SqliteTaskReadinessReviewRepository>();
        services.AddScoped<IQuarterRepository, SqliteQuarterRepository>();
        services.AddScoped<IInitiativeRepository, SqliteInitiativeRepository>();
        services.AddScoped<IAllocationRepository, SqliteAllocationRepository>();
        services.AddScoped<ISprintGoalRepository, SqliteSprintGoalRepository>();
        services.AddScoped<IInitiativeDependencyRepository, SqliteInitiativeDependencyRepository>();
        services.AddScoped<IInitiativeMemberRepository, SqliteInitiativeMemberRepository>();
        services.AddScoped<IExternalDependencyRepository, SqliteExternalDependencyRepository>();
        services.AddScoped<IQuarterMemberRepository, SqliteQuarterMemberRepository>();
        services.AddScoped<IInitiativeProductRepository, SqliteInitiativeProductRepository>();
        services.AddScoped<IProductRepository, SqliteProductRepository>();
        services.AddScoped<IProjectProductRepository, SqliteProjectProductRepository>();
        services.AddScoped<ISkillRoadmapRepository, SqliteSkillRoadmapRepository>();
        services.AddScoped<IRoadmapSkillRepository, SqliteRoadmapSkillRepository>();
        services.AddScoped<ICareerPlanRepository, SqliteCareerPlanRepository>();
        services.AddScoped<ICareerPlanSkillRepository, SqliteCareerPlanSkillRepository>();

        // Register Claude API service with HttpClient. The Anthropic SDK applies its own
        // per-request timeout and retries, so the HttpClient must not cut requests off first
        services.AddHttpClient<IClaudeApiService, ClaudeApiService>(client => client.Timeout = Timeout.InfiniteTimeSpan);

        // Register the Ollama client with HttpClient. A local model reading months of notes on
        // a laptop can take minutes, so the timeout is generous
        services.AddHttpClient<IOllamaApiService, OllamaApiService>(client => client.Timeout = TimeSpan.FromMinutes(10));

        // Register the read-only Jira Cloud client with HttpClient
        services.AddHttpClient<IJiraApiClient, JiraApiClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

        // Register the read-only Google Calendar (iCal feed) client with HttpClient. The feed's
        // address is its secret, so the factory's request logging, which logs every URL, is removed.
        // The fetched feed is held in memory for 30 minutes, shared across requests
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<GoogleCalendarFeedCache>();
        services.AddHttpClient<IGoogleCalendarClient, GoogleCalendarClient>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();

        // Register database backup service
        services.AddSingleton(sp => new DatabaseBackupService(
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseBackupService>>(),
            databaseKey: databaseKey));

        // Extract database path from connection string
        var databasePath = ExtractDatabasePath(connectionString);

        // The database is backed up at startup (DatabaseInitializer) and every hour after
        services.AddSingleton<IAutomaticBackups>(sp =>
            new DatabaseBackupStatus(sp.GetRequiredService<DatabaseBackupService>(), databasePath));
        services.AddHostedService(sp => new DatabaseBackupScheduler(
            sp.GetRequiredService<DatabaseBackupService>(),
            databasePath,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseBackupScheduler>>()));

        // Always initialize database (creates tables), optionally seed data
        services.AddSingleton<IHostedService>(sp =>
            new DatabaseInitializer(sp, databasePath, seedData, databaseKey));

        return services;
    }

    /// <summary>
    /// Extracts the database file path from a SQLite connection string.
    /// </summary>
    private static string ExtractDatabasePath(string connectionString)
    {
        // Simple extraction for "Data Source=path" format
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("Data Source=".Length).Trim();
            }
        }

        throw new ArgumentException($"Could not extract database path from connection string: {connectionString}");
    }
}

/// <summary>
/// Background service to initialize database on startup.
/// </summary>
public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _databasePath;
    private readonly bool _seedData;
    private readonly string? _databaseKey;

    public DatabaseInitializer(IServiceProvider serviceProvider, string databasePath, bool seedData = true, string? databaseKey = null)
    {
        _serviceProvider = serviceProvider;
        _databasePath = databasePath;
        _seedData = seedData;
        _databaseKey = databaseKey;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // A database written before encryption existed, and its backups, are encrypted first
        if (_databaseKey is not null)
        {
            var logger = _serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseInitializer>>();
            var encrypted = DatabaseEncryption.EncryptPlaintextFiles(_databasePath, _databaseKey, logger);
            if (encrypted > 0)
            {
                logger.LogInformation("Encrypted {Count} database file(s) that were stored unencrypted", encrypted);
            }
        }

        // Create backup before any database operations
        var backupService = _serviceProvider.GetRequiredService<DatabaseBackupService>();
        backupService.CreateBackup(_databasePath);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HiveDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync(cancellationToken);

        // Seed data if enabled and database is empty
        if (_seedData)
        {
            context.SeedData();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

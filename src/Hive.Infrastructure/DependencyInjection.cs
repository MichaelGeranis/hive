using Hive.Application.Interfaces;
using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using Hive.Infrastructure.Persistence.Repositories.Sqlite;
using Hive.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        // Register in-memory repositories
        services.AddScoped<IDirectReportRepository, DirectReportRepository>();
        services.AddScoped<IPerformanceReviewRepository, PerformanceReviewRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISkillCategoryRepository, SkillCategoryRepository>();
        services.AddScoped<ISkillAssessmentRepository, SkillAssessmentRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, OneOnOneMeetingRepository>();
        services.AddScoped<IMeetingNoteRepository, MeetingNoteRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITeamTaskRepository, TeamTaskRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();
        services.AddScoped<IManagerNoteRepository, ManagerNoteRepository>();
        services.AddScoped<INoteFolderRepository, NoteFolderRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ISprintCapacityRepository, SprintCapacityRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IParentRepository, ParentRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IChecklistTemplateRepository, ChecklistTemplateRepository>();
        services.AddScoped<IChecklistTemplateItemRepository, ChecklistTemplateItemRepository>();
        services.AddScoped<IChecklistInstanceRepository, ChecklistInstanceRepository>();
        services.AddScoped<IChecklistInstanceItemRepository, ChecklistInstanceItemRepository>();
        services.AddScoped<IProjectKnowledgeRepository, ProjectKnowledgeRepository>();
        services.AddScoped<ISentimentAnalysisCacheRepository, SentimentAnalysisCacheRepository>();
        services.AddScoped<IQuarterRepository, QuarterRepository>();
        services.AddScoped<IInitiativeRepository, InitiativeRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ISprintGoalRepository, SprintGoalRepository>();
        services.AddScoped<IInitiativeDependencyRepository, InitiativeDependencyRepository>();
        services.AddScoped<IInitiativeMemberRepository, InitiativeMemberRepository>();
        services.AddScoped<IKnowledgePointRepository, KnowledgePointRepository>();

        // Register Claude API service with HttpClient
        services.AddHttpClient<IClaudeApiService, ClaudeApiService>();

        return services;
    }

    /// <summary>
    /// Adds infrastructure services using SQLite storage (for production).
    /// </summary>
    public static IServiceCollection AddSqliteInfrastructureServices(
        this IServiceCollection services,
        string connectionString,
        bool seedData = true)
    {
        // Register HiveDbContext with SQLite
        services.AddDbContext<HiveDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register SQLite repositories
        services.AddScoped<IDirectReportRepository, SqliteDirectReportRepository>();
        services.AddScoped<IPerformanceReviewRepository, SqlitePerformanceReviewRepository>();
        services.AddScoped<ISkillRepository, SqliteSkillRepository>();
        services.AddScoped<ISkillCategoryRepository, SqliteSkillCategoryRepository>();
        services.AddScoped<ISkillAssessmentRepository, SqliteSkillAssessmentRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, SqliteOneOnOneMeetingRepository>();
        services.AddScoped<IMeetingNoteRepository, SqliteMeetingNoteRepository>();
        services.AddScoped<IProjectRepository, SqliteProjectRepository>();
        services.AddScoped<ITeamTaskRepository, SqliteTeamTaskRepository>();
        services.AddScoped<IAppSettingsRepository, SqliteAppSettingsRepository>();
        services.AddScoped<ILeaveRepository, SqliteLeaveRepository>();
        services.AddScoped<IManagerNoteRepository, SqliteManagerNoteRepository>();
        services.AddScoped<INoteFolderRepository, SqliteNoteFolderRepository>();
        services.AddScoped<ISprintRepository, SqliteSprintRepository>();
        services.AddScoped<ISprintCapacityRepository, SqliteSprintCapacityRepository>();
        services.AddScoped<IDocumentRepository, SqliteDocumentRepository>();
        services.AddScoped<IParentRepository, SqliteParentRepository>();
        services.AddScoped<IActivityRepository, SqliteActivityRepository>();
        services.AddScoped<IChecklistTemplateRepository, SqliteChecklistTemplateRepository>();
        services.AddScoped<IChecklistTemplateItemRepository, SqliteChecklistTemplateItemRepository>();
        services.AddScoped<IChecklistInstanceRepository, SqliteChecklistInstanceRepository>();
        services.AddScoped<IChecklistInstanceItemRepository, SqliteChecklistInstanceItemRepository>();
        services.AddScoped<IProjectKnowledgeRepository, SqliteProjectKnowledgeRepository>();
        services.AddScoped<ISentimentAnalysisCacheRepository, SqliteSentimentAnalysisCacheRepository>();
        services.AddScoped<IQuarterRepository, SqliteQuarterRepository>();
        services.AddScoped<IInitiativeRepository, SqliteInitiativeRepository>();
        services.AddScoped<IAllocationRepository, SqliteAllocationRepository>();
        services.AddScoped<ISprintGoalRepository, SqliteSprintGoalRepository>();
        services.AddScoped<IInitiativeDependencyRepository, SqliteInitiativeDependencyRepository>();
        services.AddScoped<IInitiativeMemberRepository, SqliteInitiativeMemberRepository>();
        services.AddScoped<IKnowledgePointRepository, SqliteKnowledgePointRepository>();

        // Register Claude API service with HttpClient
        services.AddHttpClient<IClaudeApiService, ClaudeApiService>();

        // Register database backup service
        services.AddSingleton<DatabaseBackupService>();

        // Extract database path from connection string
        var databasePath = ExtractDatabasePath(connectionString);

        // Always initialize database (creates tables), optionally seed data
        services.AddSingleton<IHostedService>(sp =>
            new DatabaseInitializer(sp, databasePath, seedData));

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

    public DatabaseInitializer(IServiceProvider serviceProvider, string databasePath, bool seedData = true)
    {
        _serviceProvider = serviceProvider;
        _databasePath = databasePath;
        _seedData = seedData;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

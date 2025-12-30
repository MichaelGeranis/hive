using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using Hive.Infrastructure.Persistence.Repositories.Sqlite;
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
        services.AddScoped<ISkillAssessmentRepository, SkillAssessmentRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, OneOnOneMeetingRepository>();
        services.AddScoped<IMeetingNoteRepository, MeetingNoteRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITeamTaskRepository, TeamTaskRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();

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
        services.AddScoped<ISkillAssessmentRepository, SqliteSkillAssessmentRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, SqliteOneOnOneMeetingRepository>();
        services.AddScoped<IMeetingNoteRepository, SqliteMeetingNoteRepository>();
        services.AddScoped<IProjectRepository, SqliteProjectRepository>();
        services.AddScoped<ITeamTaskRepository, SqliteTeamTaskRepository>();
        services.AddScoped<IAppSettingsRepository, SqliteAppSettingsRepository>();
        services.AddScoped<ILeaveRepository, SqliteLeaveRepository>();

        // Always initialize database (creates tables), optionally seed data
        services.AddSingleton<IHostedService>(sp =>
            new DatabaseInitializer(sp, seedData));

        return services;
    }
}

/// <summary>
/// Background service to initialize database on startup.
/// </summary>
public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly bool _seedData;

    public DatabaseInitializer(IServiceProvider serviceProvider, bool seedData = true)
    {
        _serviceProvider = serviceProvider;
        _seedData = seedData;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HiveDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync(cancellationToken);

        // Seed data if enabled and database is empty
        if (_seedData)
        {
            context.SeedData();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

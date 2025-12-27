using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using Hive.Infrastructure.Persistence.Repositories.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        // Initialize database and seed data
        if (seedData)
        {
            services.AddHostedService<DatabaseInitializer>();
        }

        return services;
    }
}

/// <summary>
/// Background service to initialize database on startup.
/// </summary>
public class DatabaseInitializer : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HiveDbContext>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync(cancellationToken);

        // Seed data if empty
        context.SeedData();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

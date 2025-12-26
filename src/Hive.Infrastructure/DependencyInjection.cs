using Hive.Core.Interfaces;
using Hive.Infrastructure.Persistence;
using Hive.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Infrastructure;

/// <summary>
/// Extension methods for configuring Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
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

        // Register repositories
        services.AddScoped<IDirectReportRepository, DirectReportRepository>();
        services.AddScoped<IPerformanceReviewRepository, PerformanceReviewRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISkillAssessmentRepository, SkillAssessmentRepository>();
        services.AddScoped<IOneOnOneMeetingRepository, OneOnOneMeetingRepository>();
        services.AddScoped<IMeetingNoteRepository, MeetingNoteRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITeamTaskRepository, TeamTaskRepository>();

        return services;
    }
}

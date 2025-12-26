using Hive.Application.Interfaces;
using Hive.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Application;

/// <summary>
/// Extension methods for configuring Application layer services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDirectReportService, DirectReportService>();
        services.AddScoped<IPerformanceReviewService, PerformanceReviewService>();
        services.AddScoped<ISkillService, SkillService>();
        services.AddScoped<ISkillAssessmentService, SkillAssessmentService>();
        services.AddScoped<IOneOnOneMeetingService, OneOnOneMeetingService>();
        services.AddScoped<IMeetingNoteService, MeetingNoteService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITeamTaskService, TeamTaskService>();
        return services;
    }
}

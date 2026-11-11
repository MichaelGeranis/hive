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
        services.AddScoped<IOneOnOneMeetingService, OneOnOneMeetingService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITeamTaskService, TeamTaskService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IJiraImportService, JiraImportService>();
        services.AddScoped<IJiraSyncService, JiraSyncService>();
        services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
        services.AddScoped<IManagerNoteService, ManagerNoteService>();
        services.AddScoped<INoteFolderService, NoteFolderService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<ISprintCapacityService, SprintCapacityService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IProjectKnowledgeService, ProjectKnowledgeService>();
        services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
        services.AddScoped<IRetroSummaryService, RetroSummaryService>();
        services.AddScoped<ITaskReadinessService, TaskReadinessService>();
        services.AddScoped<IQuarterlyPlanningService, QuarterlyPlanningService>();
        services.AddScoped<IPlanningBoardService, PlanningBoardService>();
        services.AddScoped<ISkillRoadmapService, SkillRoadmapService>();
        services.AddScoped<ICareerPlanService, CareerPlanService>();
        return services;
    }
}

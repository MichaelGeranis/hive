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
        services.AddScoped<ISkillCategoryService, SkillCategoryService>();
        services.AddScoped<ISkillAssessmentService, SkillAssessmentService>();
        services.AddScoped<IOneOnOneMeetingService, OneOnOneMeetingService>();
        services.AddScoped<IMeetingNoteService, MeetingNoteService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITeamTaskService, TeamTaskService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IAppSettingsService, AppSettingsService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IJiraImportService, JiraImportService>();
        services.AddScoped<IManagerNoteService, ManagerNoteService>();
        services.AddScoped<ISprintService, SprintService>();
        services.AddScoped<ISprintCapacityService, SprintCapacityService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IChecklistService, ChecklistService>();
        services.AddScoped<IProjectKnowledgeService, ProjectKnowledgeService>();
        services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
        services.AddScoped<IQuarterlyPlanningService, QuarterlyPlanningService>();
        services.AddScoped<IQuarterlyPlanningInsightsService, QuarterlyPlanningInsightsService>();
        return services;
    }
}
